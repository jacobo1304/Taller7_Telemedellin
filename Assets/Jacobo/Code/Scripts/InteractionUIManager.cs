using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InteractionUIManager : MonoBehaviour
{
    [Header("Texto feedback")]
    [SerializeField] private CanvasGroup feedbackCanvasGroup;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private float fadeInDuration = 0.35f;
    [SerializeField] private float visibleDuration = 1.8f;
    [SerializeField] private float fadeOutDuration = 0.35f;

    [Header("Texto pregunta")]
    [SerializeField] private TMP_Text questionText;

    [Header("3 imágenes de Pose options")]
    [SerializeField] private Image[] poseImages = new Image[3];

    [Header("Hold fill por opción (índices 0,1,2)")]
    [Tooltip("Asigna aquí las imágenes Fill hijas de cada opción.")]
    [SerializeField] private Image[] holdProgressFills = new Image[3];

    private Coroutine feedbackRoutine;

    private void Awake()
    {
        if (feedbackCanvasGroup != null)
        {
            feedbackCanvasGroup.alpha = 0f;
        }

        ClearHoldProgress();
    }

    public void SetQuestion(string question)
    {
        if (questionText != null)
        {
            questionText.text = question;
        }
    }

    public void ShowPoseImages(Sprite[] sprites)
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

    public void SetHoldProgressForOption(int optionIndex, float normalizedProgress)
    {
        if (holdProgressFills == null || holdProgressFills.Length == 0)
        {
            return;
        }

        float progress = Mathf.Clamp01(normalizedProgress);

        for (int i = 0; i < holdProgressFills.Length; i++)
        {
            Image fill = holdProgressFills[i];
            if (fill == null) continue;

            bool isSelected = i == optionIndex;
            fill.gameObject.SetActive(isSelected && progress > 0f);
            fill.fillAmount = isSelected ? progress : 0f;
        }
    }

    public void ClearHoldProgress()
    {
        if (holdProgressFills == null) return;

        for (int i = 0; i < holdProgressFills.Length; i++)
        {
            Image fill = holdProgressFills[i];
            if (fill == null) continue;
            fill.fillAmount = 0f;
            fill.gameObject.SetActive(false);
        }
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
