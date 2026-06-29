using UnityEngine;
using DG.Tweening;

namespace Ludocore
{
    /// <summary>Profile for VideoFocusController — exposes audio, pitch, post, and lighting tuning.</summary>
    [CreateAssetMenu(menuName = "Ludocore/Video Focus Profile", fileName = "VideoFocusProfile", order = 100)]
    public class VideoFocusProfile : ScriptableObject
    {
        //==================== AUDIO (VOLUME) =====================
        [Header("Audio — Volume")]
        [Tooltip("Target volume the video/audio fades to while focused (0..1).")]
        [Range(0f, 1f)]
        public float focusedVolume = 1f;

        [Tooltip("Seconds to fade audio volume in when focused.")]
        [Min(0f)]
        public float fadeInDuration = 1f;

        [Tooltip("Seconds to fade audio volume out when unfocused.")]
        [Min(0f)]
        public float fadeOutDuration = 1f;

        [Tooltip("Easing used for volume fades.")]
        public Ease ease = Ease.InOutSine;

        //==================== AUDIO — PITCH =====================
        [Header("Audio — Pitch")]
        [Tooltip("Seconds to ramp audio pitch from 0 → 1 when the interaction plays.")]
        [Min(0f)]
        public float pitchEntryDuration = 1f;

        [Tooltip("Time → normalized pitch curve (X = seconds, Y = 0..1).")]
        public AnimationCurve pitchEntryCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        //==================== POST-PROCESSING (VIGNETTE & SATURATION) =====================
        [Header("Post-Processing — Vignette")]
        [Tooltip("Vignette intensity when not focused (baseline).")]
        [Range(0f, 1f)]
        public float vignetteNormal = 0.15f;

        [Tooltip("Vignette intensity at full focus.")]
        [Range(0f, 1f)]
        public float vignetteGazed = 0.55f;

        [Tooltip("Time → vignette envelope (X = seconds, Y = 0..1).")]
        public AnimationCurve vignetteCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Post-Processing — Color Adjustments")]
        [Tooltip("Saturation when not focused (0 = no change).")]
        [Range(-100f, 100f)]
        public float saturationNormal = 0f;

        [Tooltip("Saturation at full focus (negative = desaturated).")]
        [Range(-100f, 100f)]
        public float saturationGazed = -100f;

        [Tooltip("Time → saturation envelope (X = seconds, Y = 0..1).")]
        public AnimationCurve saturationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Duration (seconds) used to play vignette & saturation timelines.")]
        [Min(0f)]
        public float postProcessDuration = 1.5f;

        //==================== ENVIRONMENT LIGHTING =====================
        [Header("Environment Lighting")]
        [Tooltip("Baseline ambient intensity (used as restore or reference).")]
        [Min(0f)]
        public float ambientBaseline = 0f;

        [Tooltip("Target ambient intensity multiplier when focused (0..1).")]
        [Range(0f, 1f)]
        public float ambientGazed = 1f;

        [Tooltip("Baseline reflection intensity (used as restore or reference).")]
        [Min(0f)]
        public float reflectionBaseline = 0f;

        [Tooltip("Target reflection intensity multiplier when focused (0..1).")]
        [Range(0f, 1f)]
        public float reflectionGazed = 1f;

        [Tooltip("Time → lighting envelope (X = seconds, Y = 0..1).")]
        public AnimationCurve lightingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Duration (seconds) for environment lighting timelines.")]
        [Min(0f)]
        public float lightingDuration = 1.5f;
    }
}
