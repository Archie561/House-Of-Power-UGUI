using System;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public abstract class BasePopup : MonoBehaviour
{    
    private CanvasGroup _canvasGroup;

    protected const float ANIMATION_DURATION = 0.3f;

    protected virtual void Awake() => _canvasGroup = GetComponent<CanvasGroup>();

    public virtual void Open(Action onOpened = null)
    {
        gameObject.SetActive(true);
        _canvasGroup.interactable = false;

        transform.DOScale(Vector3.one, ANIMATION_DURATION).From(Vector3.zero).SetEase(Ease.OutBack).OnComplete(() =>
        {
            _canvasGroup.interactable = true;
            onOpened?.Invoke();
        });
    }

    public virtual void Close(Action onClosed = null)
    {
        _canvasGroup.interactable = false;

        transform.DOScale(Vector3.zero, ANIMATION_DURATION).SetEase(Ease.InBack).OnComplete(() =>
        {
            gameObject.SetActive(false);
            onClosed?.Invoke();
        });
    }
}
