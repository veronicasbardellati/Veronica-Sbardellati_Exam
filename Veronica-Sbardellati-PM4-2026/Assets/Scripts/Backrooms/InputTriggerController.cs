using System;
using System.Collections;
using UnityEngine;

namespace Ludocore
{
    /// <summary>Component that exposes a single `profile` ScriptableObject parameter.
    /// The profile contains input-trigger sensor, vignette and audio tuning.
    /// This controller enforces cooldowns, plays audio and raises a vignette request event.</summary>
    public class InputTriggerController : MonoBehaviour
    {
        //==================== PROFILE =====================
        [Header("Profile")]
        [Tooltip("Reference to the InputTriggerProfile asset containing sensor, vignette and audio settings")]
        [SerializeField] private InputTriggerProfile profile;

        //==================== STATE =====================
        private float _lastTriggerTime = -Mathf.Infinity;
        private AudioSource _audioSource;

        //==================== OUTPUT =====================
        /// <summary>Raised when the controller requests a vignette change.
        /// Parameters: (targetIntensity, durationSeconds)</summary>
        public event Action<float, float> OnRequestVignette;

        // Public accessor for external code / inspector-driven wiring
        public InputTriggerProfile Profile => profile;

        //==================== LIFECYCLE =====================
        private void Awake()
        {
            if (profile == null)
            {
                Debug.LogWarning($"{nameof(InputTriggerController)} on '{gameObject.name}' has no profile assigned.");
            }

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                // Create a dedicated one to play trigger audio if the profile defines a clip
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
            }
        }

        //==================== API =====================
        /// <summary>Attempt to trigger the profile behaviour. Respects profile cooldown.</summary>
        public void Trigger()
        {
            if (profile == null) return;

            if (Time.time < _lastTriggerTime + profile.triggerCooldown) return;

            _lastTriggerTime = Time.time;

            // Request vignette change (other systems should subscribe)
            OnRequestVignette?.Invoke(profile.maxVignetteIntensity, profile.vignetteDuration);

            // Play audio per profile settings
            if (profile.triggerClip != null)
            {
                StopAllCoroutines();
                StartCoroutine(PlayAudioDelayed());
            }
        }

        /// <summary>Cancel any ongoing audio (if looping) and stop vignette requests.</summary>
        public void Cancel()
        {
            StopAllCoroutines();
            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Stop();
                _audioSource.loop = false;
            }

            // Signal zero vignette (subscribers may decide how to interpret duration=0)
            OnRequestVignette?.Invoke(0f, 0f);
        }

        //==================== PRIVATE =====================
        private IEnumerator PlayAudioDelayed()
        {
            if (profile == null || profile.triggerClip == null) yield break;

            if (profile.audioDelay > 0f)
                yield return new WaitForSeconds(profile.audioDelay);

            _audioSource.clip = profile.triggerClip;
            _audioSource.volume = profile.triggerVolume;
            _audioSource.loop = profile.loopAudio;
            _audioSource.Play();
        }
    }
}