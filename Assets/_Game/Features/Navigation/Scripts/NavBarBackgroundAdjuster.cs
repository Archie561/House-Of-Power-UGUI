using UnityEngine;

namespace Game.Features.Navigation
{
    /// <summary>
    /// Automatically adjusts the height of the navigation bar background to accommodate the device's safe area.
    /// Useful for devices with a "home indicator" (e.g., iPhone X+).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class NavBarBackgroundAdjuster : MonoBehaviour
    {
        [Tooltip("The default height of the background without safe area padding.")]
        [SerializeField] private float _baseHeight = 263f;

        private void Awake()
        {
            AdjustHeight();
        }

        private void AdjustHeight()
        {
            var rectTransform = GetComponent<RectTransform>();
            var rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;

            if (rootCanvas == null)
            {
                Debug.LogWarning("[NavBarBackgroundAdjuster] Root Canvas not found. Skipping adjustment.");
                return;
            }

            float bottomPaddingPixels = Screen.safeArea.y;
            float scaleFactor = rootCanvas.scaleFactor;

            // Prevent division by zero
            if (scaleFactor <= 0.001f) scaleFactor = 1f;

            float bottomPaddingUnits = bottomPaddingPixels / scaleFactor;

            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, _baseHeight + bottomPaddingUnits);
        }
    }
}
