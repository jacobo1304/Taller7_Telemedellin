using UnityEngine;
using UnityEngine.Events;

public abstract class InteractionActionBase : MonoBehaviour
{
    [Header("Identificación")]
    [SerializeField] private string interactionId = "interaction_1";

    [Header("Pregunta")]
    [TextArea(2, 4)]
    [SerializeField] private string questionText;

    [Header("Opciones")]
    [SerializeField] private int correctOptionIndex = 0;
    [SerializeField] private int wrongOption1Index = 1;
    [SerializeField] private int wrongOption2Index = 2;

    [Header("Mensajes")]
    [TextArea(2, 4)] [SerializeField] private string correctMessage;
    [TextArea(2, 4)] [SerializeField] private string wrongMessage1;
    [TextArea(2, 4)] [SerializeField] private string wrongMessage2;

    [Header("3 imágenes de pose correcta")]
    [SerializeField] private Sprite[] correctPoseImages = new Sprite[3];

    [Header("Eventos extra")]
    [SerializeField] private UnityEvent onCorrect;
    [SerializeField] private UnityEvent onWrong1;
    [SerializeField] private UnityEvent onWrong2;

    public string InteractionId => interactionId;
    public int CorrectOptionIndex => correctOptionIndex;
    public int WrongOption1Index => wrongOption1Index;

    public bool HandleAnswer(int selectedOptionIndex, InteractionUIManager uiManager)
    {
        if (uiManager != null)
        {
            uiManager.SetQuestion(questionText);
            uiManager.ShowCorrectPoseImages(correctPoseImages);
        }

        if (selectedOptionIndex == correctOptionIndex)
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

    protected abstract void ApplyCorrectEffect();
    protected abstract void ApplyWrongEffect1();
    protected abstract void ApplyWrongEffect2();
}
