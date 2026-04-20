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

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private float nextDebugLogTime = 0f;
    private Coroutine feedbackRoutine;

    private bool CanLogDebug()
    {
        if (!debugLogs)
        {
            return false;
        }

        if (Time.unscaledTime < nextDebugLogTime)
        {
            return false;
        }

        nextDebugLogTime = Time.unscaledTime + 1f;
        return true;
    }

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

        if (CanLogDebug())
        {
            Debug.Log($"{nameof(InteractionUIManager)}: ShowPoseImages called with spritesCount={(sprites == null ? 0 : sprites.Length)} and poseImagesCount={poseImages.Length}");
        }

        for (int i = 0; i < poseImages.Length; i++)
        {
            if (poseImages[i] == null) continue;

            Sprite sprite = (sprites != null && i < sprites.Length) ? sprites[i] : null;
            poseImages[i].sprite = sprite;
            poseImages[i].enabled = sprite != null;

            if (CanLogDebug())
            {
                Debug.Log($"{nameof(InteractionUIManager)}: poseImages[{i}] sprite={(sprite == null ? "null" : sprite.name)} enabled={poseImages[i].enabled}");
            }
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

    public float GetFeedbackSequenceDuration()
    {
        float a = Mathf.Max(0f, fadeInDuration);
        float b = Mathf.Max(0f, visibleDuration);
        float c = Mathf.Max(0f, fadeOutDuration);
        return a + b + c;
    }

    public void SetHoldProgressForOption(int optionIndex, float normalizedProgress)
    {
        if (holdProgressFills == null || holdProgressFills.Length == 0)
        {
            return;
        }

        float progress = Mathf.Clamp01(normalizedProgress);

        if (CanLogDebug())
        {
            Debug.Log($"{nameof(InteractionUIManager)}: SetHoldProgressForOption optionIndex={optionIndex} normalizedProgress={progress:F2}");
        }

        for (int i = 0; i < holdProgressFills.Length; i++)
        {
            Image fill = holdProgressFills[i];
            if (fill == null) continue;

            bool isSelected = i == optionIndex;
            fill.gameObject.SetActive(isSelected && progress > 0f);
            fill.fillAmount = isSelected ? progress : 0f;

            if (CanLogDebug())
            {
                Debug.Log($"{nameof(InteractionUIManager)}: holdProgressFills[{i}] name={fill.gameObject.name} active={fill.gameObject.activeSelf} fillAmount={fill.fillAmount:F2}");
            }
        }
    }

    public void SetupPoseUI(InteractionActionBase interaction)
    {
        if (interaction == null || interaction.PoseOptions == null)
        {
            return;
        }

        if (CanLogDebug())
        {
            Debug.Log($"{nameof(InteractionUIManager)}: SetupPoseUI with poseOptionsCount={interaction.PoseOptions.Length} and poseImagesCount={poseImages.Length}");
        }

        // Sync the pose images and fills with the InteractionActionBase pose order
        for (int i = 0; i < poseImages.Length && i < interaction.PoseOptions.Length; i++)
        {
            if (poseImages[i] != null && interaction.PoseOptions[i] != null)
            {
                poseImages[i].sprite = interaction.PoseOptions[i].poseImage;
                poseImages[i].enabled = true;

                if (CanLogDebug())
                {
                    Debug.Log($"{nameof(InteractionUIManager)}: SetupPoseUI pose[{i}] name={interaction.PoseOptions[i].poseName} sprite={(interaction.PoseOptions[i].poseImage == null ? "null" : interaction.PoseOptions[i].poseImage.name)}");
                }
            }
        }

        ClearHoldProgress();
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
