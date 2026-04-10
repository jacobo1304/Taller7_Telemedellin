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
    [Tooltip("Índice correcto por defecto (usado si la interacción no lo sobreescribe).")]
    [SerializeField] private int correctOptionIndex = 0;
    [SerializeField] private int wrongOption1Index = 1;
    [SerializeField] private int wrongOption2Index = 2;

    [Header("Mensajes")]
    [TextArea(2, 4)] [SerializeField] private string correctMessage;
    [TextArea(2, 4)] [SerializeField] private string wrongMessage1;
    [TextArea(2, 4)] [SerializeField] private string wrongMessage2;

    [Header("3 imágenes de Opcion de pose")]
    [SerializeField] private Sprite[] PoseOptions = new Sprite[3];

    [Header("Eventos extra")]
    [SerializeField] private UnityEvent onCorrect;
    [SerializeField] private UnityEvent onWrong1;
    [SerializeField] private UnityEvent onWrong2;

    public InteractionType InteractionType => interactionType;
    public int CorrectOptionIndex => ResolveCorrectOptionIndex();
    public int WrongOption1Index => wrongOption1Index;
    protected int PoseOptionsCount => PoseOptions == null ? 0 : PoseOptions.Length;

    public void PresentToUI(InteractionUIManager uiManager)
    {
        if (uiManager == null)
        {
            return;
        }

        uiManager.SetQuestion(questionText);
        uiManager.ShowPoseImages(PoseOptions);
    }

    public bool HandleAnswer(int selectedOptionIndex, InteractionUIManager uiManager)
    {
        PresentToUI(uiManager);

        int resolvedCorrectIndex = ResolveCorrectOptionIndex();

        if (selectedOptionIndex == resolvedCorrectIndex)
        {
            ApplyCorrectEffect();
            onCorrect?.Invoke();
            uiManager?.ShowFeedback(correctMessage);
            return true;
        }

        if (selectedOptionIndex == wrongOption1Index)
        {
            ApplyWrongEffect1();
            onWrong1?.Invoke();
            uiManager?.ShowFeedback(wrongMessage1);
            return false;
        }

        ApplyWrongEffect2();
        onWrong2?.Invoke();
        uiManager?.ShowFeedback(wrongMessage2);
        return false;
    }

    protected virtual int ResolveCorrectOptionIndex()
    {
        if (PoseOptionsCount <= 0)
        {
            return correctOptionIndex;
        }

        return Mathf.Clamp(correctOptionIndex, 0, PoseOptionsCount - 1);
    }

    protected abstract void ApplyCorrectEffect();
    protected abstract void ApplyWrongEffect1();
    protected abstract void ApplyWrongEffect2();
}
