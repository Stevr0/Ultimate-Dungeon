using System.Collections;
using UnityEngine;

namespace UltimateDungeon.Visuals
{
    /// <summary>
    /// HitFlash
    /// --------
    /// Simple visual feedback when damage is taken.
    /// Flashes the renderer red briefly.
    ///
    /// Client-side only.
    /// </summary>
    [DisallowMultipleComponent]
    public class HitFlash : MonoBehaviour
    {
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private Color hitColor = Color.red;
        [SerializeField] private float flashDuration = 0.1f;

        private Color[] _originalColors;
        private Coroutine _flashRoutine;

        private void Awake()
        {
            // Auto-fill renderers if not set
            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<Renderer>();

            _originalColors = new Color[renderers.Length];

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].material.HasProperty("_Color"))
                    _originalColors[i] = renderers[i].material.color;
            }
        }

        /// <summary>
        /// Call this when damage is applied.
        /// </summary>
        public void Flash()
        {
            if (_flashRoutine != null)
                StopCoroutine(_flashRoutine);

            _flashRoutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            // Apply hit color
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].material.HasProperty("_Color"))
                    renderers[i].material.color = hitColor;
            }

            yield return new WaitForSeconds(flashDuration);

            // Restore original color
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].material.HasProperty("_Color"))
                    renderers[i].material.color = _originalColors[i];
            }

            _flashRoutine = null;
        }
    }
}
