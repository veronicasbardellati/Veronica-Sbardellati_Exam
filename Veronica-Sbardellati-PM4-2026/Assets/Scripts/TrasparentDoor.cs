using UnityEngine;
using DG.Tweening;
public class TrasparentDoor : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float pitchDuration = 1f;
    [SerializeField] private AnimationCurve pitchCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Door Material")]
    [Tooltip("Renderer whose material's alpha will be animated. If left empty, the component's Renderer will be used.")]
    [SerializeField] private Renderer doorRenderer;
    [SerializeField] private float materialDuration = 1f;
    [SerializeField] private AnimationCurve materialCurve = AnimationCurve.Linear(0, 1, 1, 0);

    [Header("Collider")]
    [Tooltip("BoxCollider to disable when the interaction completes. If left empty, will try to find a BoxCollider on the same GameObject.")]
    [SerializeField] private BoxCollider doorBoxCollider;

    private Material _instancedMaterial;
    private string _colorPropertyName = null;
    private bool _played = false;

    void Start()
    {
        if (doorRenderer == null)
            doorRenderer = GetComponent<Renderer>();

        if (doorRenderer != null)
        {
            // Ensure we operate on an instance to avoid modifying shared material asset
            _instancedMaterial = doorRenderer.material;

            // Determine which color property the material uses. Prefer _BaseColor (URP/HDRP) then _Color (Built-in)
            if (_instancedMaterial.HasProperty("_BaseColor"))
                _colorPropertyName = "_BaseColor";
            else if (_instancedMaterial.HasProperty("_Color"))
                _colorPropertyName = "_Color";
            else
                Debug.LogWarning($"[{nameof(TrasparentDoor)}] Material on '{doorRenderer.gameObject.name}' has no known color property (_BaseColor/_Color). Alpha animation will be skipped.");
        }
        else
        {
            Debug.LogWarning($"[{nameof(TrasparentDoor)}] No Renderer assigned or found on '{gameObject.name}'. Material alpha animation will be skipped.");
        }

        if (doorBoxCollider == null)
            doorBoxCollider = GetComponent<BoxCollider>();
    }

    /// <summary>
    /// Entry point for the interaction. Call this from your Trigger Sensor.
    /// </summary>
    public void PlayInteraction()
    {
        if (_played)
            return;

        _played = true;
        Sequence seq = DOTween.Sequence();

        // Audio pitch animation (0 -> 1)
        if (audioSource != null)
        {
            audioSource.pitch = 0f;
            var pitchTween = DOTween.To(() => audioSource.pitch, x => audioSource.pitch = x, 1f, pitchDuration)
                                   .SetEase(pitchCurve);
            seq.Join(pitchTween);
        }
        else
        {
            Debug.LogWarning($"[{nameof(TrasparentDoor)}] AudioSource not assigned. Skipping pitch animation.");
        }

        // Material alpha animation (1 -> 0)
        if (_instancedMaterial != null && !string.IsNullOrEmpty(_colorPropertyName))
        {
            // Ensure starting alpha is 1 (opaque)
            SetMaterialAlpha(1f);
            float startAlpha() => GetMaterialAlpha();
            var matTween = DOTween.To(startAlpha, SetMaterialAlpha, 0f, materialDuration)
                                  .SetEase(materialCurve);
            seq.Join(matTween);
        }

        // When sequence completes, turn off the box collider
        seq.OnComplete(() =>
        {
            if (doorBoxCollider != null)
            {
                doorBoxCollider.enabled = false;
            }
        });

        seq.Play();
    }

    // Helper: get current alpha from the instanced material
    private float GetMaterialAlpha()
    {
        if (_instancedMaterial == null || string.IsNullOrEmpty(_colorPropertyName))
            return 1f;

        Color c = _instancedMaterial.GetColor(_colorPropertyName);
        return c.a;
    }

    // Helper: set alpha on the instanced material
    private void SetMaterialAlpha(float alpha)
    {
        if (_instancedMaterial == null || string.IsNullOrEmpty(_colorPropertyName))
            return;

        Color c = _instancedMaterial.GetColor(_colorPropertyName);
        c.a = Mathf.Clamp01(alpha);
        _instancedMaterial.SetColor(_colorPropertyName, c);

        // If the shader uses a separate _BaseMap (texture) and requires rendering mode changes to show transparency,
        // users should ensure the material's rendering mode supports transparency (e.g., Fade/Transparent).
    }

    // Convenience alias used by some trigger systems
    public void OnPlay() => PlayInteraction();
}
