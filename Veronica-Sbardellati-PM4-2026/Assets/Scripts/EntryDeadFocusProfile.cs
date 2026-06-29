using UnityEngine;

namespace Ludocore
{
    /// <summary>
    /// Profile for EntryDeadFocusController — holds tuning values referenced by the controller.
    /// </summary>
    [CreateAssetMenu(menuName = "Ludocore/Entry Dead Focus Profile", fileName = "EntryDeadFocusProfile", order = 100)]
    public class EntryDeadFocusProfile : ScriptableObject
    {
        [Header("Presence")]
        [Tooltip("Max distance (meters) considered inside the entry zone.")]
        [Min(0f)]
        public float MaxDistance = 2f;

        [Header("Stillness")]
        [Tooltip("Velocity threshold (m/s) considered 'still'.")]
        [Min(0f)]
        public float StillnessVelocityThreshold = 0.05f;

        [Tooltip("Seconds the player must remain below the stillness threshold.")]
        [Min(0f)]
        public float StillnessWindow = 2f;

        [Header("Vignette")]
        [Tooltip("Time -> vignette envelope (X = seconds, Y = 0..1).")]
        public AnimationCurve VignetteCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Seconds/sec speed used when ramping vignette up.")]
        [Min(0f)]
        public float VignetteAttackSpeed = 1f;

        [Tooltip("Seconds/sec speed used when releasing vignette.")]
        [Min(0f)]
        public float VignetteReleaseSpeed = 1f;

        [Tooltip("Max vignette intensity applied by the controller.")]
        [Range(0f, 1f)]
        public float VignetteMax = 0.5f;

        [Header("Audio")]
        [Tooltip("Time -> audio envelope (X = seconds, Y = 0..1).")]
        public AnimationCurve AudioCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Attack speed for audio gain.")]
        [Min(0f)]
        public float AudioAttackSpeed = 1f;

        [Tooltip("Release speed for audio gain.")]
        [Min(0f)]
        public float AudioReleaseSpeed = 1f;

        [Tooltip("Maximum audio gain applied to the baseline volume (0..1).")]
        [Range(0f, 1f)]
        public float AudioMaxGain = 1f;

        [Header("Audio — Lowpass")]
        [Tooltip("Lowpass cutoff used when audio is fully engaged.")]
        [Min(0f)]
        public float AudioLowpassCutoffMin = 800f;

        [Tooltip("Lowpass cutoff used when audio is not engaged.")]
        [Min(0f)]
        public float AudioLowpassCutoffMax = 8000f;
    }
}