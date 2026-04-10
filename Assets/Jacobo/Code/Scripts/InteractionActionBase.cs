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
    // La interacción define cuál es la opción correcta.
    // Los demás índices se derivan automáticamente.

    [Header("Mensajes")]
    [TextArea(2, 4)] [SerializeField] private string correctMessage;
    [TextArea(2, 4)] [SerializeField] private string wrongMessage1;
    [TextArea(2, 4)] [SerializeField] private string wrongMessage2;

    [Header("3 imágenes de Opcion de pose")]
    [SerializeField] public PoseData[] PoseOptions = new PoseData[3];

    [Header("Eventos extra")]
    [SerializeField] private UnityEvent onCorrect;
    [SerializeField] private UnityEvent onWrong1;
    [SerializeField] private UnityEvent onWrong2;

    public InteractionType InteractionType => interactionType;
    public int CorrectOptionIndex => ResolveCorrectOptionIndex();
    public int WrongOption1Index => GetWrongOptionIndex(1);
    public int WrongOption2Index => GetWrongOptionIndex(2);
    protected int PoseOptionsCount => PoseOptions == null ? 0 : PoseOptions.Length;

    public void PresentToUI(InteractionUIManager uiManager)
    {
        if (uiManager == null)
        {
            return;
        }

        uiManager.SetQuestion(questionText);

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

        int resolvedCorrectIndex = ResolveCorrectOptionIndex();
        int resolvedWrong1Index = WrongOption1Index;
        int resolvedWrong2Index = WrongOption2Index;

        if (selectedOptionIndex == resolvedCorrectIndex)
        {
            onCorrect?.Invoke();
            uiManager?.ShowFeedback(correctMessage);
            return true;
        }

        if (selectedOptionIndex == resolvedWrong1Index)
        {
            onWrong1?.Invoke();
            uiManager?.ShowFeedback(wrongMessage1);
            return false;
        }

        if (selectedOptionIndex == resolvedWrong2Index)
        {
            onWrong2?.Invoke();
            uiManager?.ShowFeedback(wrongMessage2);
            return false;
        }

        onWrong2?.Invoke();
        uiManager?.ShowFeedback(wrongMessage2);
        return false;
    }

    protected virtual int ResolveCorrectOptionIndex()
    {
        if (PoseOptionsCount <= 0)
        {
            return 0;
        }

        return 0;
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

    public void PreviewOption(int selectedOptionIndex)
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

    protected abstract void ApplyCorrectEffect();
    protected abstract void ApplyWrongEffect1();
    protected abstract void ApplyWrongEffect2();
}
