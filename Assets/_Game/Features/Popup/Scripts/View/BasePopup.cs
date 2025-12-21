using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public abstract class BasePopup : MonoBehaviour
{
    // Віртуальний метод відкриття (кожен попап може мати свої аргументи Setup)
    public virtual void Open()
    {
        gameObject.SetActive(true);
        transform.DOScale(Vector3.one, 0.3f).From(Vector3.zero).SetEase(Ease.OutBack);
    }

    public virtual void Close()
    {
        transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
        {
            gameObject.SetActive(false);
        });
    }
}
