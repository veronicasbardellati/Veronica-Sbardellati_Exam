using UnityEngine;

namespace Ludocore
{
    /// <summary>Tuning profile for an input-triggered sensor interaction — sensor thresholds, vignette and audio.</summary>
    [CreateAssetMenu(menuName = "Ludocore/Input Trigger Profile")]
    public class InputTriggerProfile : ScriptableObject
    {
        //==================== INPUT / SENSOR =====================
        [Header("Input — Sensor")]
        [Tooltip("Maximum detection distance — proximity is normalized against this value")]
        [Min(0.01f)]
        public float sensorMaxDistance = 5f;

        [Tooltip("If >0, input must be held this long (seconds). 0 = press")]
        [Min(0f)]
        public float holdDuration = 0f;

        [Tooltip("Cooldown after a trigger before it can fire again (seconds)")]
        [Min(0f)]
        public float triggerCooldown = 0f;

        //==================== VIGNETTE =====================
        [Header("Vignette")]
        [Tooltip("Time→vignette remapping curve (X = normalized time 0..1, Y = normalized intensity 0..1)")]
        public AnimationCurve vignetteCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Tooltip("Vignette intensity at full trigger (0..1)")]
        [Range(0f, 1f)]
        public float maxVignetteIntensity = 0.4f;

        [Tooltip("Duration of vignette ramp (seconds)")]
        [Min(0f)]
        public float vignetteDuration = 1f;

        //==================== AUDIO =====================
        [Header("Audio")]
        [Tooltip("Audio clip played when triggered")]
        public AudioClip triggerClip;

        [Tooltip("Playback volume (0..1)")]
        [Range(0f, 1f)]
        public float triggerVolume = 1f;

        [Tooltip("Delay before playing audio (seconds)")]
        [Min(0f)]
        public float audioDelay = 0f;

        [Tooltip("Loop audio while active")]
        public bool loopAudio = false;
    }
}