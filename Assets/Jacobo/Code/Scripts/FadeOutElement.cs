using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class FadeOutElement : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField, Min(0f)] private float defaultDuration = 0.35f;
    [SerializeField] private Ease ease = Ease.OutQuad;
    [SerializeField] private bool controlRaycast = true;

    private Tween fadeTween;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    private void OnDestroy()
    {
        KillTween();
    }

    public void FadeIn()
    {
        Debug.Log($"Fading in {gameObject.name}");
        FadeTo(1f, defaultDuration, true);
    }

    public void FadeOut()
    {
        FadeTo(0f, defaultDuration, false);
    }

    public void FadeTo(float alpha)
    {
        bool interact = alpha > 0.95f;
        FadeTo(alpha, defaultDuration, interact);
    }

    public void FadeTo(float alpha, float duration, bool interactable)
    {
        if (canvasGroup == null)
        {
            return;
        }

        KillTween();

        float target = Mathf.Clamp01(alpha);
        fadeTween = canvasGroup.DOFade(target, Mathf.Max(0f, duration))
            .SetEase(ease)
            .SetUpdate(true)
            .OnUpdate(() =>
            {
                if (!controlRaycast)
                {
                    return;
                }

                bool canInteract = canvasGroup.alpha > 0.95f && interactable;
                canvasGroup.interactable = canInteract;
                canvasGroup.blocksRaycasts = canInteract;
            })
            .OnComplete(() =>
            {
                if (!controlRaycast)
                {
                    return;
                }

                canvasGroup.interactable = interactable && target > 0.95f;
                canvasGroup.blocksRaycasts = interactable && target > 0.95f;
            });
    }

    public void SetVisible(bool visible)
    {
        SetVisible(visible, visible);
    }

    public void SetVisible(bool visible, bool interactable)
    {
        if (canvasGroup == null)
        {
            return;
        }

        KillTween();

        canvasGroup.alpha = visible ? 1f : 0f;

        if (controlRaycast)
        {
            canvasGroup.interactable = visible && interactable;
            canvasGroup.blocksRaycasts = visible && interactable;
        }
    }

    private void KillTween()
    {
        if (fadeTween == null)
        {
            return;
        }

        if (fadeTween.IsActive())
        {
            fadeTween.Kill();
        }

        fadeTween = null;
    }
}
