using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class NavBarBackgroundAdjuster : MonoBehaviour
{
    private const float BASE_HEIGHT = 263f;

    private void Awake()
    {
        var rectTransform = GetComponent<RectTransform>();

        float bottomPaddingPixels = Screen.safeArea.y;

        Canvas rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
        float scaleFactor = rootCanvas.scaleFactor;

        if (scaleFactor == 0) scaleFactor = 1;

        float bottomPaddingUnits = bottomPaddingPixels / scaleFactor;

        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, BASE_HEIGHT + bottomPaddingUnits);
    }
}
