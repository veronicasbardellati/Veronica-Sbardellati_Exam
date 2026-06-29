using System.Collections;
using UnityEngine;

namespace Ludocore
{
    /// <summary>
    /// Sensor-driven one-shot scale pulse hidden under a global light dim.
    /// Flow: Dim → wait → scale (to targetScale) → (optionally) scale back → Brighten → cooldown.
    /// Lighting is delegated to LightManager (this component only decides WHEN to switch state).
    /// </summary>
    public class ScaleLoop : MonoBehaviour
    {
        //==================== SCENE REFERENCES =====================
        [Header("Scene References")]
        [Tooltip("Sensor whose first-detection triggers the scale pulse (e.g. a TriggerSensor)")]
        [SerializeField] private Sensor sensor;

        [Tooltip("Transform to scale. This is the explicit target for the pulse and is required.")]
        [SerializeField] private Transform target;

        [Tooltip("Light manager to drive. Falls back to LightManager.Instance if empty.")]
        [SerializeField] private LightManager lightManager;

        [Tooltip("Optional profile that overrides the LightManager's default for THIS pulse's blackout.")]
        [SerializeField] private LightManagerProfile lightProfileOverride;

        //==================== SCALE SETTINGS =====================
        [Header("Scale")]
        [Tooltip("Scale to reach during the pulse (localScale).")]
        [SerializeField] private Vector3 targetScale = new Vector3(1.5f, 1.5f, 1.5f);

        [Tooltip("If true the component will tween the target back to its original scale after the pulse.")]
        [SerializeField] private bool returnToOriginal = true;

        [Tooltip("Seconds to scale up to targetScale.")]
        [Min(0f)]
        [SerializeField] private float scaleUpDuration = 0.25f;

        [Tooltip("Seconds to scale back to original scale.")]
        [Min(0f)]
        [SerializeField] private float scaleDownDuration = 0.25f;

        //==================== TIMING =====================
        [Header("Timing")]
        [Tooltip("Seconds to wait after Dim() before performing the scale.")]
        [Min(0f)]
        [SerializeField] private float darknessDelay = 0.1f;

        [Tooltip("Seconds to ignore further detections after a pulse completes.")]
        [Min(0f)]
        [SerializeField] private float reEnableCooldown = 0.5f;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private bool isScaling;
        [ReadOnly, SerializeField] private bool onCooldown;

        private bool _hadSignal;
        private float _cooldownUntil;

        private Transform _pendingTarget;
        private Coroutine _scaleCoroutine;
        private Transform _activeTarget;
        private Vector3 _activeOriginalScale;

        //==================== LIFECYCLE =====================
        private void Update()
        {
            if (!sensor) return;

            bool hasSignal = sensor.TryGetNearest(out var nearest);

            onCooldown = Time.time < _cooldownUntil;

            if (hasSignal && !_hadSignal && !isScaling && !onCooldown)
                TriggerScale(nearest);

            _hadSignal = hasSignal;
        }

        //==================== PRIVATE =====================
        private void TriggerScale(Signal nearest)
        {
            if (!target) return;

            isScaling = true;

            var lm = Manager;
            if (lm) lm.Dim(lightProfileOverride);

            var resolved = ResolveTarget(nearest);
            if (!resolved) // resolved may be null if neither explicit target nor nearest is available
            {
                isScaling = false;
                return;
            }

            _pendingTarget = resolved;
            Invoke(nameof(PerformScale), darknessDelay);
        }

        private void PerformScale()
        {
            if (_pendingTarget)
            {
                // start the scale pulse coroutine
                _scaleCoroutine = StartCoroutine(ScalePulseCoroutine(_pendingTarget));
            }

            _pendingTarget = null;
        }

        private IEnumerator ScalePulseCoroutine(Transform t)
        {
            _activeTarget = t;
            _activeOriginalScale = t.localScale;

            // scale up
            if (scaleUpDuration <= 0f)
            {
                t.localScale = targetScale;
            }
            else
            {
                float elapsed = 0f;
                var from = _activeOriginalScale;
                var to = targetScale;
                while (elapsed < scaleUpDuration)
                {
                    elapsed += Time.deltaTime;
                    float p = Mathf.Clamp01(elapsed / scaleUpDuration);
                    t.localScale = Vector3.Lerp(from, to, p);
                    yield return null;
                }

                t.localScale = to;
            }

            // scale down (optional)
            if (returnToOriginal)
            {
                if (scaleDownDuration <= 0f)
                {
                    t.localScale = _activeOriginalScale;
                }
                else
                {
                    float elapsed = 0f;
                    var from = t.localScale;
                    var to = _activeOriginalScale;
                    while (elapsed < scaleDownDuration)
                    {
                        elapsed += Time.deltaTime;
                        float p = Mathf.Clamp01(elapsed / scaleDownDuration);
                        t.localScale = Vector3.Lerp(from, to, p);
                        yield return null;
                    }

                    t.localScale = to;
                }
            }

            // brighten and finish
            var lm = Manager;
            if (lm) lm.Brighten(lightProfileOverride);

            _cooldownUntil = Time.time + reEnableCooldown;
            isScaling = false;

            _scaleCoroutine = null;
            _activeTarget = null;
        }

        private Transform ResolveTarget(Signal nearest)
        {
            // primary: explicit `target` field. If null, fall back to the nearest signaled object's transform.
            if (target) return target;
            return nearest.Object ? nearest.Object.transform : null;
        }

        private LightManager Manager => lightManager ? lightManager : LightManager.Instance;

        private void OnDisable()
        {
            CancelInvoke(nameof(PerformScale));

            if (_scaleCoroutine != null)
            {
                StopCoroutine(_scaleCoroutine);
                _scaleCoroutine = null;
            }

            // restore scale if we interrupted while returning-to-original was expected
            if (_activeTarget && returnToOriginal)
            {
                _activeTarget.localScale = _activeOriginalScale;
            }

            isScaling = false;
            _pendingTarget = null;
            _activeTarget = null;
        }
    }
}
