using UnityEngine;

namespace Game.General.Helpers
{
    /// <summary>
    /// Adjusts the RectTransform anchors to fit within the device's Safe Area.
    /// Useful for handling notches, rounded corners, and home bars on mobile devices.
    /// Best attached to a full-screen "Container" panel inside the Canvas.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private void Awake()
        {
            var rectTransform = GetComponent<RectTransform>();
            Rect safeArea = Screen.safeArea;

            // Convert Safe Area pixels to normalized coordinates (0 to 1)
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            // Apply anchors
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;

            // Reset offsets so the rect snaps to the new anchors
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}