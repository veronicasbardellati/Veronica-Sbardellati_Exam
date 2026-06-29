using UnityEngine;

namespace Ludocore
{
    /// <summary>Swaps and reverts a texture on a shared material.</summary>
    public class MaterialTextureSwapper : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("Shared material to modify (changes persist across play sessions)")]
        [SerializeField] private Material sharedMaterial;
        [Tooltip("Shader texture property name (e.g. _MainTex, _BaseMap)")]
        [SerializeField] private string propertyName = "_BaseMap";
        [Tooltip("Texture applied by SwapTexture")]
        [SerializeField] private Texture swappedTexture;

        //==================== STATE =====================
        private Texture _originalTexture;
        private bool _cached;

        //==================== LIFECYCLE =====================
        private void Awake()
        {
            CacheOriginal();
        }

        private void OnDisable()
        {
            RevertTexture();
        }

        //==================== INPUTS =====================
        /// <summary>Apply the configured swapped texture to the shared material.</summary>
        public void SwapTexture()
        {
            if (!CacheOriginal()) return;
            sharedMaterial.SetTexture(propertyName, swappedTexture);
        }

        /// <summary>Restore the original texture captured at Awake.</summary>
        public void RevertTexture()
        {
            if (!_cached || !sharedMaterial) return;
            sharedMaterial.SetTexture(propertyName, _originalTexture);
        }

        //==================== PRIVATE =====================
        private bool CacheOriginal()
        {
            if (_cached) return true;
            if (!sharedMaterial || !sharedMaterial.HasProperty(propertyName)) return false;

            _originalTexture = sharedMaterial.GetTexture(propertyName);
            _cached = true;
            return true;
        }
    }
}
