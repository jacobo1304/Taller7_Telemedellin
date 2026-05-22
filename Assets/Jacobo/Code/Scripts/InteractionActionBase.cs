using UnityEngine;
using UnityEngine.Events;

public abstract class InteractionActionBase : MonoBehaviour
{
    [Header("Identificación")]
    [SerializeField] private InteractionType interactionType = InteractionType.Titulares;

    [Header("Pregunta")]
    [TextArea(2, 4)]
    [SerializeField] private string questionText;

    [Header("Opciones")]
    [Tooltip("Índice de la opción correcta (0,1,2...). Las incorrectas se derivan automáticamente.")]
    [SerializeField] private int winningPoseOptionIndex = 0;

    [Header("Mensajes")]
    [TextArea(2, 4)] [SerializeField] private string correctMessage;
    [TextArea(2, 4)] [SerializeField] private string wrongMessage1;
    [TextArea(2, 4)] [SerializeField] private string wrongMessage2;
    [TextArea(2, 4)] [SerializeField] private string defaultMessage;

    [Header("Opciones de pose (cantidad variable)")]
    [SerializeField] public PoseData[] PoseOptions = new PoseData[0];

    [Header("Eventos extra")]
    [SerializeField] private UnityEvent onCorrect;
    [SerializeField] private UnityEvent onWrong1;
    [SerializeField] private UnityEvent onWrong2;
    [SerializeField] private UnityEvent onHoldComplete;

    private Coroutine holdCompleteRoutine;
    private int lastSelectedOptionIndex = -1;

    public InteractionType InteractionType => interactionType;
    public int CorrectOptionIndex => ResolveCorrectOptionIndex();
    public int WrongOption1Index => GetWrongOptionIndex(1);
    public int WrongOption2Index => GetWrongOptionIndex(2);
    public int StoredSelectedOptionIndex => lastSelectedOptionIndex;
    public bool HasStoredSelection => lastSelectedOptionIndex >= 0;
    protected int PoseOptionsCount => PoseOptions == null ? 0 : PoseOptions.Length;

    public void PresentToUI(InteractionUIManager uiManager)
    {
        if (uiManager == null)
        {
            return;
        }

        uiManager.SetQuestion(questionText ?? string.Empty);

        if (PoseOptions == null || PoseOptions.Length == 0)
        {
            uiManager.ShowPoseImages(null);
            return;
        }

        // Pass the sprites from PoseData to the UI Manager
        Sprite[] sprites = new Sprite[PoseOptions.Length];
        for (int i = 0; i < PoseOptions.Length; i++)
        {
            sprites[i] = PoseOptions[i]?.poseImage;
        }
        uiManager.ShowPoseImages(sprites);
    }

    public bool HandleAnswer(int selectedOptionIndex, InteractionUIManager uiManager)
    {
        PresentToUI(uiManager);
        lastSelectedOptionIndex = selectedOptionIndex;

        int resolvedCorrectIndex = ResolveCorrectOptionIndex();
        int resolvedWrong1Index = WrongOption1Index;
        int resolvedWrong2Index = WrongOption2Index;

        if (selectedOptionIndex == resolvedCorrectIndex)
        {
            ApplyCorrectEffect();
            onCorrect?.Invoke();
            ShowFeedbackIfAny(uiManager, correctMessage);
            float soundDuration = SoundManager.Instance != null ? SoundManager.Instance.PlayPositiveFeedback() : 0f;
            ScheduleHoldComplete(uiManager, correctMessage, soundDuration);
            return true;
        }

        if (selectedOptionIndex == resolvedWrong1Index)
        {
            ApplyWrongEffect1();
            onWrong1?.Invoke();
            ShowFeedbackIfAny(uiManager, wrongMessage1);
            float soundDuration = SoundManager.Instance != null ? SoundManager.Instance.PlayNegativeFeedback() : 0f;
            ScheduleHoldComplete(uiManager, wrongMessage1, soundDuration);
            return false;
        }

        if (selectedOptionIndex == resolvedWrong2Index)
        {
            ApplyWrongEffect2();
            onWrong2?.Invoke();
            ShowFeedbackIfAny(uiManager, wrongMessage2);
            float soundDuration = SoundManager.Instance != null ? SoundManager.Instance.PlayNegativeFeedback() : 0f;
            ScheduleHoldComplete(uiManager, wrongMessage2, soundDuration);
            return false;
        }

        ApplyWrongEffect2();
        onWrong2?.Invoke();
        ShowFeedbackIfAny(uiManager, wrongMessage2);
        float soundDurationDefault = SoundManager.Instance != null ? SoundManager.Instance.PlayNegativeFeedback() : 0f;
        ScheduleHoldComplete(uiManager, wrongMessage2, soundDurationDefault);
        return false;
    }

    public bool HandleDefaultAnswer(InteractionUIManager uiManager)
    {
        PresentToUI(uiManager);
        lastSelectedOptionIndex = -1;

        ApplyDefaultEffect();
        ShowFeedbackIfAny(uiManager, defaultMessage);

        float soundDuration = SoundManager.Instance != null ? SoundManager.Instance.PlayNoPoseFeedback() : 0f;
        ScheduleHoldComplete(uiManager, defaultMessage, soundDuration);
        return false;
    }

    private static void ShowFeedbackIfAny(InteractionUIManager uiManager, string message)
    {
        if (uiManager == null || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        uiManager.ShowFeedback(message);
    }

    protected virtual int ResolveCorrectOptionIndex()
    {
        if (PoseOptionsCount <= 0)
        {
            return Mathf.Max(0, winningPoseOptionIndex);
        }

        return Mathf.Clamp(winningPoseOptionIndex, 0, PoseOptionsCount - 1);
    }

    protected int GetWrongOptionIndex(int wrongNumber)
    {
        int correctIndex = ResolveCorrectOptionIndex();
        if (PoseOptionsCount <= 0 || wrongNumber < 1)
        {
            return -1;
        }

        int found = 0;
        for (int i = 0; i < PoseOptionsCount; i++)
        {
            if (i == correctIndex)
            {
                continue;
            }

            found++;
            if (found == wrongNumber)
            {
                return i;
            }
        }

        return -1;
    }

    public virtual void PreviewOption(int selectedOptionIndex)
    {
        int resolvedCorrectIndex = ResolveCorrectOptionIndex();
        int resolvedWrong1Index = WrongOption1Index;
        int resolvedWrong2Index = WrongOption2Index;

        if (selectedOptionIndex == resolvedCorrectIndex)
        {
            ApplyCorrectEffect();
            return;
        }

        if (selectedOptionIndex == resolvedWrong1Index)
        {
            ApplyWrongEffect1();
            return;
        }

        if (selectedOptionIndex == resolvedWrong2Index)
        {
            ApplyWrongEffect2();
            return;
        }

        ResetHoldEffects();
    }

    public virtual void ResetHoldEffects()
    {
        // Override in derived interactions if needed.
    }

    public virtual void RestoreStoredSelection()
    {
        if (!HasStoredSelection)
        {
            return;
        }

        int resolvedCorrectIndex = ResolveCorrectOptionIndex();
        int resolvedWrong1Index = WrongOption1Index;
        int resolvedWrong2Index = WrongOption2Index;

        if (lastSelectedOptionIndex == resolvedCorrectIndex)
        {
            ApplyCorrectEffect();
            return;
        }

        if (lastSelectedOptionIndex == resolvedWrong1Index)
        {
            ApplyWrongEffect1();
            return;
        }

        if (lastSelectedOptionIndex == resolvedWrong2Index)
        {
            ApplyWrongEffect2();
            return;
        }

        ApplyWrongEffect2();
    }

    private void ScheduleHoldComplete(InteractionUIManager uiManager, string feedbackMessage, float soundDuration = 0f)
    {
        if (holdCompleteRoutine != null)
        {
            StopCoroutine(holdCompleteRoutine);
        }

        float waitTime = 0f;
        if (uiManager != null && !string.IsNullOrWhiteSpace(feedbackMessage))
        {
            waitTime = uiManager.GetFeedbackSequenceDuration(soundDuration);
        }
        else if (soundDuration > 0f)
        {
            waitTime = soundDuration;
        }

        holdCompleteRoutine = StartCoroutine(InvokeHoldCompleteAfterDelay(waitTime));
    }

    private System.Collections.IEnumerator InvokeHoldCompleteAfterDelay(float waitTime)
    {
        if (waitTime > 0f)
        {
            yield return new WaitForSeconds(waitTime);
        }

        onHoldComplete?.Invoke();
        holdCompleteRoutine = null;
    }

    protected abstract void ApplyCorrectEffect();
    protected abstract void ApplyWrongEffect1();
    protected abstract void ApplyWrongEffect2();
    protected virtual void ApplyDefaultEffect() { }
}
