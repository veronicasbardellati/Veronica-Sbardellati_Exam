using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ludocore
{
    /// <summary>
    /// Drives the EntryDeadFocus interaction:
    /// presence + stillness ? unbound vignette + audio timeline.
    /// </summary>
    public class EntryDeadFocusController : MonoBehaviour
    {
        //==================== SCENE REFERENCES =====================
        [Header("Scene References")]
        [Tooltip("Proximity sensor that watches the entry zone (tagged Player)")]
        [SerializeField] private ProximitySensor proximitySensor;

        [Tooltip("Optional TriggerSensor for entry detection")]
        [SerializeField] private TriggerSensor optionalTrigger;

        [Tooltip("AudioSource with a looping low-frequency drone (volume controlled by this controller)")]
        [SerializeField] private AudioSource audioSource;

        [Tooltip("Optional AudioLowPassFilter component attached to the audioSource (used for muffling).")]
        [SerializeField] private AudioLowPassFilter audioLowPassFilter;

        [Tooltip("Post-process Volume containing a Vignette override (URP)")]
        [SerializeField] private Volume postVolume;

        [Tooltip("Optional transform used to compute player velocity. If empty, uses Signal.Object transform when available.")]
        [SerializeField] private Transform playerRoot;

        //==================== PROFILE =====================
        [Header("Profile")]
        [Tooltip("Component holding tuning values for the EntryDeadFocus interaction")]
        [SerializeField] private EntryDeadFocusProfile profile;

        //==================== RUNTIME STATE =====================
        private Vignette _vignette;
        private float _vignetteBaseline;
        private float _vignetteCurrent;

        private float _audioBaselineVolume;
        private float _audioCurrentGain;
        private float _audioBaselineLowpass;
        private bool _audioHasLowpass;

        private Vector3 _lastPlayerPosition;
        private bool _haveLastPos;

        private float _stillnessAccum;
        private bool _triggered;
        private float _timelineTime;
        private float _timelineDuration;
        private float _retriggerCooldown = 0.5f;
        private float _cooldownTimer;

        //==================== LIFECYCLE =====================
        private void Start()
        {
            if (postVolume && postVolume.profile != null)
            {
                if (!postVolume.profile.TryGet<Vignette>(out _vignette))
                {
                    Debug.LogWarning($"[{nameof(EntryDeadFocusController)}] No Vignette override found in assigned Volume.");
                    _vignette = null;
                }
            }

            if (_vignette != null)
                _vignetteBaseline = _vignette.intensity.value;
            else
                _vignetteBaseline = 0f;

            if (audioSource != null)
            {
                _audioBaselineVolume = audioSource.volume;
                if (audioLowPassFilter == null)
                    audioLowPassFilter = audioSource.GetComponent<AudioLowPassFilter>();
                _audioHasLowpass = audioLowPassFilter != null;
                if (_audioHasLowpass)
                    _audioBaselineLowpass = audioLowPassFilter.cutoffFrequency;
                else
                    _audioBaselineLowpass = profile != null ? profile.AudioLowpassCutoffMin : 8000f;

                if (!audioSource.isPlaying)
                    audioSource.Play();
                audioSource.volume = _audioBaselineVolume;
            }

            _timelineTime = 0f;
            _triggered = false;
            _stillnessAccum = 0f;
            _cooldownTimer = 0f;
            _haveLastPos = false;

            _timelineDuration = ComputeTimelineDuration();
            if (_timelineDuration <= 0f)
                _timelineDuration = 2f;
        }

        private void Update()
        {
            if (profile == null) return;

            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.deltaTime;
            }

            if (_triggered)
            {
                AdvanceTimeline();
                return;
            }

            if (_cooldownTimer > 0f) return;

            Signal nearest;
            bool inside = false;
            if (proximitySensor != null && proximitySensor.TryGetNearest(out nearest))
            {
                inside = nearest.Distance <= profile.MaxDistance;
            }
            else if (optionalTrigger != null && optionalTrigger.HasDetections)
            {
                inside = true;
            }

            if (!inside)
            {
                _stillnessAccum = 0f;
                _haveLastPos = false;
                return;
            }

            Transform t = playerRoot;
            if (t == null && proximitySensor != null && proximitySensor.TryGetNearest(out nearest))
            {
                if (nearest.Object != null)
                    t = nearest.Object.transform;
            }

            float speed = 0f;
            if (t != null)
            {
                if (!_haveLastPos)
                {
                    _lastPlayerPosition = t.position;
                    _haveLastPos = true;
                    speed = 0f;
                }
                else
                {
                    Vector3 delta = t.position - _lastPlayerPosition;
                    speed = delta.magnitude / Mathf.Max(Time.deltaTime, 1e-6f);
                    _lastPlayerPosition = t.position;
                }
            }
            else
            {
                _stillnessAccum = 0f;
                return;
            }

            if (speed <= profile.StillnessVelocityThreshold)
            {
                _stillnessAccum += Time.deltaTime;
            }
            else
            {
                _stillnessAccum = 0f;
            }

            if (_stillnessAccum >= profile.StillnessWindow)
            {
                StartTimeline();
            }
        }

        private void OnDisable()
        {
            if (_vignette != null)
                _vignette.intensity.Override(_vignetteBaseline);
            if (audioSource != null)
                audioSource.volume = _audioBaselineVolume;
            if (audioLowPassFilter != null)
                audioLowPassFilter.cutoffFrequency = _audioBaselineLowpass;
        }

        //==================== TIMELINE =====================
        private void StartTimeline()
        {
            _triggered = true;
            _timelineTime = 0f;
            _timelineDuration = ComputeTimelineDuration();
            if (_timelineDuration <= 0f) _timelineDuration = 2f;

            if (_vignette != null)
                _vignetteBaseline = _vignette.intensity.value;
            if (audioSource != null)
                _audioBaselineVolume = audioSource.volume;
            if (audioLowPassFilter != null)
                _audioBaselineLowpass = audioLowPassFilter.cutoffFrequency;
        }

        private void AdvanceTimeline()
        {
            _timelineTime += Time.deltaTime;

            float vVal = profile.VignetteCurve.Evaluate(Mathf.Min(_timelineTime, GetCurveDuration(profile.VignetteCurve)));
            float aVal = profile.AudioCurve.Evaluate(Mathf.Min(_timelineTime, GetCurveDuration(profile.AudioCurve)));

            float vignetteTarget = vVal;
            _vignetteCurrent = Smooth(_vignetteCurrent, vignetteTarget,
                                      profile.VignetteAttackSpeed, profile.VignetteReleaseSpeed);

            float audioTarget = aVal;
            _audioCurrentGain = Smooth(_audioCurrentGain, audioTarget,
                                       profile.AudioAttackSpeed, profile.AudioReleaseSpeed);

            if (_vignette != null)
            {
                float applied = _vignetteBaseline + (_vignetteCurrent * profile.VignetteMax);
                _vignette.intensity.Override(applied);
            }

            if (audioSource != null)
            {
                audioSource.volume = _audioBaselineVolume + (_audioCurrentGain * profile.AudioMaxGain);
            }

            if (audioLowPassFilter != null)
            {
                float cutoff = Mathf.Lerp(profile.AudioLowpassCutoffMin, profile.AudioLowpassCutoffMax, aVal);
                audioLowPassFilter.cutoffFrequency = cutoff;
            }

            float maxCurveTime = Mathf.Max(GetCurveDuration(profile.VignetteCurve), GetCurveDuration(profile.AudioCurve));
            if (_timelineTime >= maxCurveTime)
            {
                StartRestore();
            }
        }

        private void StartRestore()
        {
            _triggered = false;
            _stillnessAccum = 0f;
            _cooldownTimer = _retriggerCooldown;
        }

        private float ComputeTimelineDuration()
        {
            float t1 = GetCurveDuration(profile.VignetteCurve);
            float t2 = GetCurveDuration(profile.AudioCurve);
            return Mathf.Max(t1, t2);
        }

        private static float GetCurveDuration(AnimationCurve curve)
        {
            if (curve == null || curve.keys == null || curve.keys.Length == 0) return 0f;
            return curve.keys[curve.keys.Length - 1].time;
        }

        // Smooth helper copied/adapted from ColumnControllerV2
        private static float Smooth(float current, float target, float attackSpeed, float releaseSpeed)
        {
            float speed = (target >= current) ? attackSpeed : releaseSpeed;
            if (speed <= 0f) return target;
            return Mathf.MoveTowards(current, target, speed * Time.deltaTime);
        }
    }
}