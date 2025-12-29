using System;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public abstract class BasePopup : MonoBehaviour
{    
    private CanvasGroup _canvasGroup;

    protected const float ANIMATION_DURATION = 0.3f;

    public bool IsCloseOnOverlayAllowed { get; protected set; } = true;

    protected virtual void Awake() => _canvasGroup = GetComponent<CanvasGroup>();

    public virtual void Open(Action onOpened = null)
    {
        transform.DOKill();
        gameObject.SetActive(true);
        _canvasGroup.blocksRaycasts = false;

        transform.DOScale(Vector3.one, ANIMATION_DURATION).From(Vector3.zero).SetEase(Ease.OutBack).OnComplete(() =>
        {
            _canvasGroup.blocksRaycasts = true;
            onOpened?.Invoke();
        });
    }

    public virtual void Close(Action onClosed = null)
    {
        transform.DOKill();
        _canvasGroup.blocksRaycasts = false;

        transform.DOScale(Vector3.zero, ANIMATION_DURATION).SetEase(Ease.InBack).OnComplete(() =>
        {
            gameObject.SetActive(false);
            onClosed?.Invoke();
        });
    }
}
