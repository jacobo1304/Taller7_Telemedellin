using UnityEngine;
using TMPro;
using System.Collections;

public class HeadersInteraction : InteractionActionBase
{
    [Header("Panel")]
    [SerializeField] private GameObject headerPanel;
    [SerializeField] private bool hidePanelOnDisable = true;

    [Header("Texto Headers")]
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private CanvasGroup headerCanvasGroup;

    [Header("Presets de texto por opción")]
    [TextArea(2, 4)] [SerializeField] private string correctHeaderText;
    [TextArea(2, 4)] [SerializeField] private string wrongHeader1Text;
    [TextArea(2, 4)] [SerializeField] private string wrongHeader2Text;
    [TextArea(2, 4)] [SerializeField] private string noPoseHeaderText = "";

    [Header("Fade Transition")]
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.3f;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.3f;

    private Coroutine textTransitionRoutine;
    private string currentText = "";
    private bool hasPendingReset = false;

    private void Awake()
    {
        if (headerPanel == null && headerText != null)
        {
            headerPanel = headerText.transform.parent != null ? headerText.transform.parent.gameObject : null;
        }

        if (headerCanvasGroup != null)
        {
            headerCanvasGroup.alpha = 1f;
        }

        currentText = headerText != null ? headerText.text : "";
    }

    private void OnEnable()
    {
        SetPanelVisible(true);

        if (headerCanvasGroup != null)
        {
            headerCanvasGroup.alpha = 1f;
        }

        if (hasPendingReset)
        {
            hasPendingReset = false;
            ResetHoldEffects();
        }
    }

    private void OnDisable()
    {
        if (textTransitionRoutine != null)
        {
            StopCoroutine(textTransitionRoutine);
            textTransitionRoutine = null;
        }

        hasPendingReset = false;

        if (hidePanelOnDisable)
        {
            SetPanelVisible(false);
        }
    }

    public override void ResetHoldEffects()
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            hasPendingReset = true;
            return;
        }

        ScheduleTextTransition(noPoseHeaderText, fadeInAfterChange: false);
    }

    public override void PreviewOption(int selectedOptionIndex)
    {
        int resolvedCorrectIndex = ResolveCorrectOptionIndex();
        int resolvedWrong1Index = WrongOption1Index;
        int resolvedWrong2Index = WrongOption2Index;

        string targetText = "";

        if (selectedOptionIndex == resolvedCorrectIndex)
        {
            targetText = correctHeaderText;
        }
        else if (selectedOptionIndex == resolvedWrong1Index)
        {
            targetText = wrongHeader1Text;
        }
        else if (selectedOptionIndex == resolvedWrong2Index)
        {
            targetText = wrongHeader2Text;
        }

        // For preview we want the text to update immediately at start of hold:
        ScheduleTextTransitionPreview(targetText);
    }

    protected override void ApplyCorrectEffect()
    {
        string targetText = correctHeaderText;
        ScheduleTextTransition(targetText, fadeInAfterChange: true);
    }

    protected override void ApplyWrongEffect1()
    {
        string targetText = wrongHeader1Text;
        ScheduleTextTransition(targetText, fadeInAfterChange: true);
    }

    protected override void ApplyWrongEffect2()
    {
        string targetText = wrongHeader2Text;
        ScheduleTextTransition(targetText, fadeInAfterChange: true);
    }

    private void ScheduleTextTransition(string newText, bool fadeInAfterChange)
    {
        string resolvedText = newText ?? string.Empty;
        if (resolvedText == currentText)
        {
            return;
        }

        SetPanelVisible(true);

        if (textTransitionRoutine != null)
        {
            StopCoroutine(textTransitionRoutine);
        }

        textTransitionRoutine = StartCoroutine(TextTransitionRoutine(resolvedText, fadeInAfterChange));
    }

    private void ScheduleTextTransitionPreview(string newText)
    {
        string resolvedText = newText ?? string.Empty;
        if (resolvedText == currentText)
        {
            return;
        }

        SetPanelVisible(true);

        if (textTransitionRoutine != null)
        {
            StopCoroutine(textTransitionRoutine);
            textTransitionRoutine = null;
        }

        // Immediately set the text and fade in from 0 so user sees it during the hold.
        if (headerText != null)
        {
            headerText.text = resolvedText;
            currentText = resolvedText;
        }

        if (headerCanvasGroup != null)
        {
            headerCanvasGroup.alpha = 0f;
            textTransitionRoutine = StartCoroutine(FadeInOnlyRoutine(fadeInDuration));
        }
    }

    private IEnumerator FadeInOnlyRoutine(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);
            if (headerCanvasGroup != null)
            {
                headerCanvasGroup.alpha = alpha;
            }
            yield return null;
        }

        if (headerCanvasGroup != null)
        {
            headerCanvasGroup.alpha = 1f;
        }

        textTransitionRoutine = null;
    }

    private IEnumerator TextTransitionRoutine(string newText, bool fadeInAfterChange)
    {
        SetPanelVisible(true);

        // Fade out
        float t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            float alpha = fadeOutDuration <= 0f ? 0f : 1f - Mathf.Clamp01(t / fadeOutDuration);
            if (headerCanvasGroup != null)
            {
                headerCanvasGroup.alpha = alpha;
            }
            yield return null;
        }

        if (headerCanvasGroup != null)
        {
            headerCanvasGroup.alpha = 0f;
        }

        // Change text
        if (headerText != null)
        {
            headerText.text = newText;
            currentText = newText;
        }

        if (!fadeInAfterChange)
        {
            if (headerCanvasGroup != null)
            {
                headerCanvasGroup.alpha = 0f;
            }

            // If the new text is empty (no-pose), keep the panel active but clear the text.
            if (string.IsNullOrEmpty(newText))
            {
                if (headerText != null)
                {
                    headerText.text = string.Empty;
                    currentText = string.Empty;
                }
            }

            textTransitionRoutine = null;
            yield break;
        }

        // Fade in
        t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            float alpha = fadeInDuration <= 0f ? 1f : Mathf.Clamp01(t / fadeInDuration);
            if (headerCanvasGroup != null)
            {
                headerCanvasGroup.alpha = alpha;
            }
            yield return null;
        }

        if (headerCanvasGroup != null)
        {
            headerCanvasGroup.alpha = 1f;
        }

        textTransitionRoutine = null;
    }

    private void SetPanelVisible(bool visible)
    {
        if (headerPanel != null)
        {
            headerPanel.SetActive(visible);
        }
    }
}
