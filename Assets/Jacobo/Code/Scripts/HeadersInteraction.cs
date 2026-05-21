using UnityEngine;
using TMPro;
using System.Collections;

public class HeadersInteraction : InteractionActionBase
{
    [Header("Panel")]
    [SerializeField] private GameObject headerPanel;
    [SerializeField] private bool hidePanelOnDisable = true;
    [SerializeField] private bool allowPanelActivation = true; // CAMBIO: Flag para permitir/bloquear activación del panel

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
    private Coroutine hidePanelRoutine;

    private string currentText = "";
    private bool hasPendingReset = false;

    private void Awake()
    {
        if (headerPanel == null && headerText != null)
        {
            headerPanel = headerText.transform.parent != null
                ? headerText.transform.parent.gameObject
                : null;
        }

        if (headerCanvasGroup != null)
        {
            headerCanvasGroup.alpha = 1f;
        }

        currentText = headerText != null ? headerText.text : "";
    }

    private void OnEnable()
    {
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

        if (hidePanelRoutine != null)
        {
            StopCoroutine(hidePanelRoutine);
            hidePanelRoutine = null;
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

        // CAMBIO: Pasar true para indicar que se debe desactivar el panel después del fade
        ScheduleTextTransition(noPoseHeaderText, fadeInAfterChange: false, shouldHidePanelAfter: true);
    }

    public string GetTextForOption(int optionIndex)
    {
        int resolvedCorrectIndex = ResolveCorrectOptionIndex();
        int resolvedWrong1Index = WrongOption1Index;
        int resolvedWrong2Index = WrongOption2Index;

        if (optionIndex == resolvedCorrectIndex)
        {
            return correctHeaderText ?? string.Empty;
        }

        if (optionIndex == resolvedWrong1Index)
        {
            return wrongHeader1Text ?? string.Empty;
        }

        if (optionIndex == resolvedWrong2Index)
        {
            return wrongHeader2Text ?? string.Empty;
        }

        return noPoseHeaderText ?? string.Empty;
    }

    public string GetStoredSelectedHeaderText()
    {
        return HasStoredSelection
            ? GetTextForOption(StoredSelectedOptionIndex)
            : (noPoseHeaderText ?? string.Empty);
    }

    // CAMBIO: Nuevo método público para bloquear la activación del panel en escenas específicas
    public void DisablePanelActivation()
    {
        allowPanelActivation = false;
        if (headerPanel != null)
        {
            headerPanel.SetActive(false);
        }
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

        ScheduleTextTransitionPreview(targetText);
    }

    protected override void ApplyCorrectEffect()
    {
        ScheduleTextTransition(correctHeaderText, true);
    }

    protected override void ApplyWrongEffect1()
    {
        ScheduleTextTransition(wrongHeader1Text, true);
    }

    protected override void ApplyWrongEffect2()
    {
        ScheduleTextTransition(wrongHeader2Text, true);
    }

    private void ScheduleTextTransition(string newText, bool fadeInAfterChange, bool shouldHidePanelAfter = false)
    {
        string resolvedText = newText ?? string.Empty;

        if (resolvedText == currentText)
        {
            // CAMBIO: Si el texto es igual pero se debe ocultarse el panel, forzar ocultamiento
            if (shouldHidePanelAfter && hidePanelOnDisable)
            {
                if (hidePanelRoutine != null)
                {
                    StopCoroutine(hidePanelRoutine);
                    hidePanelRoutine = null;
                }
                hidePanelRoutine = StartCoroutine(HidePanelAfterFade());
            }
            return;
        }

        // Si iba a ocultarse, cancelar ocultado
        if (hidePanelRoutine != null)
        {
            StopCoroutine(hidePanelRoutine);
            hidePanelRoutine = null;
        }

        SetPanelVisible(true);

        if (textTransitionRoutine != null)
        {
            StopCoroutine(textTransitionRoutine);
        }

        // CAMBIO: Pasar shouldHidePanelAfter a la corrutina
        textTransitionRoutine =
            StartCoroutine(TextTransitionRoutine(resolvedText, fadeInAfterChange, shouldHidePanelAfter));
    }

    private void ScheduleTextTransitionPreview(string newText)
    {
        string resolvedText = newText ?? string.Empty;

        if (resolvedText == currentText)
        {
            return;
        }

        // Si iba a ocultarse, cancelar ocultado
        if (hidePanelRoutine != null)
        {
            StopCoroutine(hidePanelRoutine);
            hidePanelRoutine = null;
        }

        SetPanelVisible(true);

        if (textTransitionRoutine != null)
        {
            StopCoroutine(textTransitionRoutine);
            textTransitionRoutine = null;
        }

        if (headerText != null)
        {
            headerText.text = resolvedText;
            currentText = resolvedText;
        }

        if (headerCanvasGroup != null)
        {
            headerCanvasGroup.alpha = 0f;
            textTransitionRoutine =
                StartCoroutine(FadeInOnlyRoutine(fadeInDuration));
        }
    }

    private IEnumerator FadeInOnlyRoutine(float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;

            float alpha = duration <= 0f
                ? 1f
                : Mathf.Clamp01(t / duration);

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

    private IEnumerator TextTransitionRoutine(
        string newText,
        bool fadeInAfterChange,
        bool shouldHidePanelAfter = false)  // CAMBIO: Nuevo parámetro para desactivar después
    {
        SetPanelVisible(true);

        // Fade out
        float t = 0f;

        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;

            float alpha = fadeOutDuration <= 0f
                ? 0f
                : 1f - Mathf.Clamp01(t / fadeOutDuration);

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

        // Cambiar texto
        if (headerText != null)
        {
            headerText.text = newText;
            currentText = newText;
        }

        if (!fadeInAfterChange)
        {
            textTransitionRoutine = null;
            
            // CAMBIO: Si debe desactivarse después del fade, iniciar corrutina de ocultamiento
            if (shouldHidePanelAfter && hidePanelOnDisable)
            {
                if (hidePanelRoutine != null)
                {
                    StopCoroutine(hidePanelRoutine);
                }
                hidePanelRoutine = StartCoroutine(HidePanelAfterFade());
            }
            
            yield break;
        }

        // Fade in
        t = 0f;

        while (t < fadeInDuration)
        {
            t += Time.deltaTime;

            float alpha = fadeInDuration <= 0f
                ? 1f
                : Mathf.Clamp01(t / fadeInDuration);

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

    // CAMBIO: Nueva corrutina que desactiva el panel INMEDIATAMENTE sin esperar extra
    // Esto evita race conditions cuando se hacen interacciones rápidas
    private IEnumerator HidePanelAfterFade()
    {
        // Solo limpiamos el texto y desactivamos el panel
        if (headerText != null)
        {
            headerText.text = string.Empty;
            currentText = string.Empty;
        }

        SetPanelVisible(false);
        hidePanelRoutine = null;
        yield break;
    }

    private IEnumerator HidePanelDelayed()
    {
        yield return new WaitForSeconds(fadeOutDuration);

        if (headerText != null)
        {
            headerText.text = string.Empty;
            currentText = string.Empty;
        }

        SetPanelVisible(false);

        hidePanelRoutine = null;
    }

    private void SetPanelVisible(bool visible)
    {
        // CAMBIO: Si se intenta activar pero está bloqueado, ignorar
        if (visible && !allowPanelActivation)
        {
            return;
        }

        if (headerPanel != null)
        {
            headerPanel.SetActive(visible);
        }
    }
}