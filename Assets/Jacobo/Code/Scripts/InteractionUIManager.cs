using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class InteractionUIManager : MonoBehaviour
{
    [Header("Texto feedback")]
    [SerializeField] private CanvasGroup feedbackCanvasGroup;
    [SerializeField] private Text feedbackText;
    [SerializeField] private float fadeInDuration = 0.35f;
    [SerializeField] private float visibleDuration = 1.8f;
    [SerializeField] private float fadeOutDuration = 0.35f;

    [Header("Texto pregunta")]
    [SerializeField] private Text questionText;

    [Header("3 imágenes pose correcta")]
    [SerializeField] private Image[] poseImages = new Image[3];

    private Coroutine feedbackRoutine;

    private void Awake()
    {
        if (feedbackCanvasGroup != null)
        {
            feedbackCanvasGroup.alpha = 0f;
        }
    }

    public void SetQuestion(string question)
    {
        if (questionText != null)
        {
            questionText.text = question;
        }
    }

    public void ShowCorrectPoseImages(Sprite[] sprites)
    {
        if (poseImages == null) return;

        for (int i = 0; i < poseImages.Length; i++)
        {
            if (poseImages[i] == null) continue;

            Sprite sprite = (sprites != null && i < sprites.Length) ? sprites[i] : null;
            poseImages[i].sprite = sprite;
            poseImages[i].enabled = sprite != null;
        }
    }

    public void ShowFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }

        if (feedbackCanvasGroup == null)
        {
            return;
        }

        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
        }

        feedbackRoutine = StartCoroutine(FeedbackRoutine());
    }

    private IEnumerator FeedbackRoutine()
    {
        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            feedbackCanvasGroup.alpha = fadeInDuration <= 0f ? 1f : Mathf.Clamp01(t / fadeInDuration);
            yield return null;
        }

        feedbackCanvasGroup.alpha = 1f;
        yield return new WaitForSeconds(visibleDuration);

        t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            feedbackCanvasGroup.alpha = fadeOutDuration <= 0f ? 0f : 1f - Mathf.Clamp01(t / fadeOutDuration);
            yield return null;
        }

        feedbackCanvasGroup.alpha = 0f;
        feedbackRoutine = null;
    }
}
