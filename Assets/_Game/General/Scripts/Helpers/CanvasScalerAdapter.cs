using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasScaler))]
public class CanvasScalerAdapter : MonoBehaviour
{
    private void Awake()
    {
        var scaler = GetComponent<CanvasScaler>();
        if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            return;

        float screenRatio = (float)Screen.width / Screen.height;
        float referenceRatio = scaler.referenceResolution.x / scaler.referenceResolution.y;

        if (screenRatio > referenceRatio)
        {
            scaler.matchWidthOrHeight = 1f;
        }
        else
        {
            scaler.matchWidthOrHeight = 0f;
        }
    }
}