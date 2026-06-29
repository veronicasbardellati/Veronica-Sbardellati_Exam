using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DG.Tweening;

namespace Ludocore
{
    public class TriggerEntryController : MonoBehaviour
    {
        //==================== SCENE REFERENCES =====================
        [Header("Scene References")]
        [Tooltip("Trigger sensor that detects the player (assign a TriggerSensor)")]
        [SerializeField] private Sensor triggerSensor;

        [Tooltip("Audio source whose pitch is animated")]
        [SerializeField] private AudioSource audioSource;

        [Tooltip("URP Volume containing Vignette and ColorAdjustments overrides")]
        [SerializeField] private Volume postProcessVolume;

        //==================== AUDIO — PITCH =====================
        [Header("Audio — Pitch")]
        [Tooltip("Seconds to ramp audio pitch from 0 -> 1")]
        [Min(0f)]
        [SerializeField] private float pitchEntryDuration = 1f;

        [Tooltip("Curve used to animate pitch (X = seconds, Y = 0..1)")]
        [SerializeField] private AnimationCurve pitchEntryCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        //==================== POST — VIGNETTE & COLOR =====================
        [Header("Post — Vignette & Desaturation")]
        [Tooltip("Target vignette intensity (0..1) applied by the interaction")]
        [Range(0f, 1f)]
        [SerializeField] private float vignetteTargetIntensity = 0.45f;

        [Tooltip("Curve used to animate vignette intensity (X = seconds, Y = 0..1)")]
        [SerializeField] private AnimationCurve vignetteCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Target color saturation applied by the interaction (-100..100)")]
        [Range(-100f, 100f)]
        [SerializeField] private float saturationTarget = -50f;

        [Tooltip("Curve used to animate color adjustments / saturation (X = seconds, Y = 0..1)")]
        [SerializeField] private AnimationCurve saturationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Duration (seconds) for vignette & saturation animations")]
        [Min(0f)]
        [SerializeField] private float postProcessDuration = 1.5f;

        //==================== ENVIRONMENT LIGHTING =====================
        [Header("Environment Lighting")]
        [Tooltip("Target multiplier for RenderSettings.ambientIntensity (0..1)")]
        [Range(0f, 1f)]
        [SerializeField] private float ambientTarget = 1f;

        [Tooltip("Target multiplier for RenderSettings.reflectionIntensity (0..1)")]
        [Range(0f, 1f)]
        [SerializeField] private float reflectionTarget = 1f;

        [Tooltip("Curve used to animate environment lighting (X = seconds, Y = 0..1)")]
        [SerializeField] private AnimationCurve lightingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Duration (seconds) for environment lighting animations")]
        [Min(0f)]
        [SerializeField] private float lightingDuration = 1.5f;

        //==================== STATE =====================
        private Vignette _vignette;
        private ColorAdjustments _colorAdjustments;
        private float _baseAudioPitch;
        private float _baseAudioVolume;
        private float _baseAmbient;
        private float _baseReflection;

        private Tween _pitchTween;
        private Tween _vignetteTween;
        private Tween _saturationTween;
        private Tween _ambientTween;
        private Tween _reflectionTween;

        //==================== LIFECYCLE =====================
        private void Awake()
        {
            if (postProcessVolume != null && postProcessVolume.profile != null)
            {
                postProcessVolume.profile.TryGet(out _vignette);
                postProcessVolume.profile.TryGet(out _colorAdjustments);
            }

            if (audioSource != null)
            {
                _baseAudioPitch = audioSource.pitch;
                _baseAudioVolume = audioSource.volume;
            }

            _baseAmbient = RenderSettings.ambientIntensity;
            _baseReflection = RenderSettings.reflectionIntensity;
        }

        private void OnEnable()
        {
            if (triggerSensor != null)
                triggerSensor.OnSignalAdded += HandleDetected;
        }

        private void OnDisable()
        {
            if (triggerSensor != null)
                triggerSensor.OnSignalAdded -= HandleDetected;
        }

        private void OnDestroy()
        {
            _pitchTween?.Kill();
            _vignetteTween?.Kill();
            _saturationTween?.Kill();
            _ambientTween?.Kill();
            _reflectionTween?.Kill();
        }

        //==================== API =====================
        [ContextMenu("Play")]
        public void Play()
        {
            if (audioSource == null && postProcessVolume == null) return;

            _pitchTween?.Kill();
            _vignetteTween?.Kill();
            _saturationTween?.Kill();
            _ambientTween?.Kill();
            _reflectionTween?.Kill();

            if (audioSource != null)
            {
                audioSource.pitch = 0f;
                _pitchTween = DOTween.To(
                    () => audioSource.pitch,
                    x => audioSource.pitch = x,
                    1f,
                    pitchEntryDuration
                ).SetEase(ConvertCurveToEase(pitchEntryCurve));
            }

            if (_vignette != null && vignetteCurve != null)
            {
                float vignetteCurveDuration = GetCurveDuration(vignetteCurve);
                _vignetteTween = DOTween.To(
                    () => 0f,
                    t => _vignette.intensity.Override(Mathf.Clamp01(Mathf.Lerp(_vignette.intensity.value, vignetteTargetIntensity, t))),
                    1f,
                    postProcessDuration
                ).SetEase(ConvertCurveToEase(vignetteCurve))
                 .OnUpdate(() =>
                 {
                     float evalTime = Mathf.Min(_vignetteTween.Elapsed(false), vignetteCurveDuration);
                     float curveVal = vignetteCurve.Evaluate(vignetteCurveDuration > 0 ? (evalTime / vignetteCurveDuration) * vignetteCurveDuration : 0f);
                     _vignette.intensity.Override(Mathf.Clamp01(curveVal * vignetteTargetIntensity));
                 });
            }

            if (_colorAdjustments != null && saturationCurve != null)
            {
                float satCurveDuration = GetCurveDuration(saturationCurve);
                _saturationTween = DOTween.To(
                    () => 0f,
                    t => _colorAdjustments.saturation.Override(Mathf.Lerp(0f, saturationTarget, t)),
                    1f,
                    postProcessDuration
                ).SetEase(ConvertCurveToEase(saturationCurve))
                 .OnUpdate(() =>
                 {
                     float evalTime = Mathf.Min(_saturationTween.Elapsed(false), satCurveDuration);
                     float curveVal = saturationCurve.Evaluate(satCurveDuration > 0 ? (evalTime / satCurveDuration) * satCurveDuration : 0f);
                     _colorAdjustments.saturation.Override(Mathf.Lerp(0f, saturationTarget, curveVal));
                 });
            }

            _ambientTween = DOTween.To(
                () => RenderSettings.ambientIntensity,
                v => RenderSettings.ambientIntensity = v,
                ambientTarget,
                lightingDuration
            ).SetEase(ConvertCurveToEase(lightingCurve));

            _reflectionTween = DOTween.To(
                () => RenderSettings.reflectionIntensity,
                v => RenderSettings.reflectionIntensity = v,
                reflectionTarget,
                lightingDuration
            ).SetEase(ConvertCurveToEase(lightingCurve));
        }

        //==================== PRIVATE HELPERS =====================
        private void HandleDetected(Signal signal)
        {
            Play();
        }

        private static float GetCurveDuration(AnimationCurve curve)
        {
            if (curve == null || curve.keys == null || curve.length == 0) return 0f;
            return curve.keys[curve.length - 1].time;
        }

        private static Ease ConvertCurveToEase(AnimationCurve curve)
        {
            if (curve == null) return Ease.Linear;
            if (curve.length == 2)
            {
                var first = curve.keys[0];
                var last = curve.keys[1];
                if (first.value == 0f && last.value == 1f)
                {
                    if (first.outTangent == 0f && last.inTangent == 0f) return Ease.InOutSine;
                    if (first.outTangent > 0f && last.inTangent < 0f) return Ease.Linear;
                }
            }
            return Ease.Linear;
        }
    }
}