using UnityEngine;
using UnityEngine.UI;

namespace Game.General.Helpers
{
    /// <summary>
    /// Automatically adjusts the CanvasScaler match mode based on the device's aspect ratio.
    /// Ensures that UI elements remain within the screen boundaries on different devices (tablets vs phones).
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public class CanvasScalerAdapter : MonoBehaviour
    {
        private void Awake()
        {
            var scaler = GetComponent<CanvasScaler>();

            // Safety check
            if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                return;
            }

            float screenRatio = (float)Screen.width / Screen.height;
            float referenceRatio = scaler.referenceResolution.x / scaler.referenceResolution.y;

            // If the current screen is "wider" (relative to reference), match height to prevent cutting off top/bottom.
            // If the screen is "taller/narrower", match width to prevent cutting off sides.
            if (screenRatio > referenceRatio)
            {
                scaler.matchWidthOrHeight = 1f; // Match Height
            }
            else
            {
                scaler.matchWidthOrHeight = 0f; // Match Width
            }
        }
    }
}