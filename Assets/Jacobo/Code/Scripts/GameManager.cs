using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private InteractionUIManager uiManager;
    [SerializeField] private AnswerHandler answerHandler;
    [SerializeField] private CustomPoseDetector customPoseDetector;
    [SerializeField] private GameObject panelPregunta;
    [SerializeField] private GameObject viewportContainer;

    [Header("Interaction Camera")]
    [Tooltip("Cámara Cinemachine por defecto para el bloque de interacciones (post-cinemática).")]
    [SerializeField] private MonoBehaviour defaultInteractionVirtualCamera;
    [SerializeField] private int interactionCameraPriority = 40;
    [SerializeField] private int interactionCameraInactivePriority = 0;

    [Header("Flow")]
    [SerializeField] private List<InteractionActionBase> interactionOrder = new List<InteractionActionBase>();
    [SerializeField] private int startIndex = 0;
    [SerializeField] private bool waitForStartInteractionsSignal = true;
    [SerializeField] private bool autoAdvanceOnCorrect = true;
    [SerializeField, Min(0f)] private float extraDelayAfterFeedback = 0f;
    [SerializeField] private bool onlyCurrentInteractionActive = true;

    private int currentIndex = -1;
    private int queuedNextIndex = -1;
    private Coroutine delayedAdvanceRoutine;
    private bool interactionsStarted = false;

    private void Awake()
    {
        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<InteractionUIManager>();
        }

        if (answerHandler == null)
        {
            answerHandler = FindFirstObjectByType<AnswerHandler>();
        }

        if (customPoseDetector == null)
        {
            customPoseDetector = FindFirstObjectByType<CustomPoseDetector>();
        }
    }

    private void OnEnable()
    {
        if (answerHandler != null)
        {
            answerHandler.onAnswerCorrect.AddListener(HandleCorrectAnswer);
            answerHandler.onAnswerWrong.AddListener(HandleWrongAnswer);
        }
    }

    private void Start()
    {
        if (interactionOrder.Count == 0)
        {
            Debug.LogWarning($"{nameof(GameManager)}: No hay interacciones configuradas en 'interactionOrder'.", this);
            return;
        }

        PrepareWaitingState();

        if (!waitForStartInteractionsSignal)
        {
            StartInteractions();
        }
    }

    private void OnDisable()
    {
        if (answerHandler != null)
        {
            answerHandler.onAnswerCorrect.RemoveListener(HandleCorrectAnswer);
            answerHandler.onAnswerWrong.RemoveListener(HandleWrongAnswer);
        }

        SetCameraPriority(defaultInteractionVirtualCamera, interactionCameraInactivePriority);

        if (delayedAdvanceRoutine != null)
        {
            StopCoroutine(delayedAdvanceRoutine);
            delayedAdvanceRoutine = null;
        }
    }

    public void NextInteraction()
    {
        if (!interactionsStarted)
        {
            Debug.LogWarning($"{nameof(GameManager)}: NextInteraction llamado antes de StartInteractions().", this);
            return;
        }

        if (panelPregunta != null)
        {
            panelPregunta.SetActive(true);
        }

        if (viewportContainer != null)
        {
            viewportContainer.SetActive(true);
        }

        ContinueAfterCinematic();
    }

    public void PreviousInteraction()
    {
        SetCurrentInteraction(currentIndex - 1);
    }

    // Método público para futuro uso desde Director/Timeline.
    // Si hay una interacción en cola, avanza a esa; si no, avanza a la siguiente inmediata.
    public void ContinueAfterCinematic()
    {
        if (!interactionsStarted)
        {
            Debug.LogWarning($"{nameof(GameManager)}: ContinueAfterCinematic llamado antes de StartInteractions().", this);
            return;
        }

        if (queuedNextIndex >= 0)
        {
            int target = queuedNextIndex;
            queuedNextIndex = -1;
            SetCurrentInteraction(target);
            return;
        }

        if (currentIndex >= 0 && currentIndex < interactionOrder.Count - 1)
        {
            SetCurrentInteraction(currentIndex + 1);
        }
    }

    public void SetCurrentInteraction(int index)
    {
        if (!interactionsStarted)
        {
            Debug.LogWarning($"{nameof(GameManager)}: SetCurrentInteraction bloqueado hasta StartInteractions().", this);
            return;
        }

        if (interactionOrder.Count == 0)
        {
            return;
        }

        if (delayedAdvanceRoutine != null)
        {
            StopCoroutine(delayedAdvanceRoutine);
            delayedAdvanceRoutine = null;
        }

        queuedNextIndex = -1;

        int clampedIndex = Mathf.Clamp(index, 0, interactionOrder.Count - 1);
        currentIndex = clampedIndex;

        InteractionActionBase current = interactionOrder[currentIndex];
        if (current == null)
        {
            Debug.LogWarning($"{nameof(GameManager)}: La interacción en índice {currentIndex} es null.", this);
            return;
        }

        if (onlyCurrentInteractionActive)
        {
            for (int i = 0; i < interactionOrder.Count; i++)
            {
                InteractionActionBase item = interactionOrder[i];
                if (item != null)
                {
                    item.gameObject.SetActive(i == currentIndex);
                }
            }
        }

        answerHandler?.SetInputLocked(false);
        customPoseDetector?.SetResponseLock(false);
        customPoseDetector?.SetCurrentInteraction(current);
        current.PresentToUI(uiManager);
        uiManager?.ClearHoldProgress();
    }

    // Llamar desde Signal/Director para iniciar el flujo de interacciones.
    public void StartInteractions()
    {
        if (interactionOrder.Count == 0)
        {
            Debug.LogWarning($"{nameof(GameManager)}: No hay interacciones configuradas en 'interactionOrder'.", this);
            return;
        }

        if (interactionsStarted)
        {
            return;
        }

        interactionsStarted = true;
        if (panelPregunta != null)
        {
            panelPregunta.SetActive(true);
        }

        if (viewportContainer != null)
        {
            viewportContainer.SetActive(true);
        }

        SetCameraPriority(defaultInteractionVirtualCamera, interactionCameraPriority);

        int clampedStart = Mathf.Clamp(startIndex, 0, interactionOrder.Count - 1);
        SetCurrentInteraction(clampedStart);
    }

    // Utilidad para reiniciar desde cinemática si necesitas re-jugar el flujo.
    public void ResetAndWaitForStartSignal()
    {
        interactionsStarted = false;
        currentIndex = -1;
        queuedNextIndex = -1;

        if (delayedAdvanceRoutine != null)
        {
            StopCoroutine(delayedAdvanceRoutine);
            delayedAdvanceRoutine = null;
        }

        PrepareWaitingState();
    }

    private void HandleCorrectAnswer(InteractionType type)
    {
        HandleAnyAnswer(type);
    }

    private void HandleWrongAnswer(InteractionType type)
    {
        HandleAnyAnswer(type);
    }

    private void HandleAnyAnswer(InteractionType type)
    {
        if (!interactionsStarted)
        {
            return;
        }

        if (currentIndex < 0 || currentIndex >= interactionOrder.Count)
        {
            return;
        }

        InteractionActionBase current = interactionOrder[currentIndex];
        if (current == null)
        {
            return;
        }

        if (current.InteractionType == type && currentIndex < interactionOrder.Count - 1)
        {
            if (queuedNextIndex >= 0)
            {
                return;
            }

            queuedNextIndex = currentIndex + 1;
            answerHandler?.SetInputLocked(true);
            customPoseDetector?.SetResponseLock(true);

            if (!autoAdvanceOnCorrect)
            {
                return;
            }

            if (delayedAdvanceRoutine != null)
            {
                StopCoroutine(delayedAdvanceRoutine);
            }

            delayedAdvanceRoutine = StartCoroutine(AdvanceAfterFeedbackRoutine());
        }
    }

    private System.Collections.IEnumerator AdvanceAfterFeedbackRoutine()
    {
        float waitTime = extraDelayAfterFeedback;
        if (uiManager != null)
        {
            waitTime += uiManager.GetFeedbackSequenceDuration();
        }

        if (waitTime > 0f)
        {
            yield return new WaitForSeconds(waitTime);
        }

        ContinueAfterCinematic();
        delayedAdvanceRoutine = null;
    }

    private void PrepareWaitingState()
    {
        answerHandler?.SetInputLocked(true);
        customPoseDetector?.SetResponseLock(true);
        SetCameraPriority(defaultInteractionVirtualCamera, interactionCameraInactivePriority);

        if (panelPregunta != null)
        {
            panelPregunta.SetActive(false);
        }

        if (viewportContainer != null)
        {
            viewportContainer.SetActive(false);
        }

        if (onlyCurrentInteractionActive)
        {
            for (int i = 0; i < interactionOrder.Count; i++)
            {
                InteractionActionBase item = interactionOrder[i];
                if (item != null)
                {
                    item.gameObject.SetActive(false);
                }
            }
        }

        uiManager?.ClearHoldProgress();
    }

    private static void SetCameraPriority(MonoBehaviour cameraComponent, int priority)
    {
        if (cameraComponent == null)
        {
            return;
        }

        var type = cameraComponent.GetType();

        var priorityProp = type.GetProperty("Priority");
        if (priorityProp != null && priorityProp.CanWrite)
        {
            if (TrySetPriorityValueOnMember(cameraComponent, priorityProp.PropertyType, priorityProp.GetValue(cameraComponent), priority, out object updatedPropValue))
            {
                priorityProp.SetValue(cameraComponent, updatedPropValue);
                return;
            }
        }

        var priorityField = type.GetField("m_Priority");
        if (priorityField != null)
        {
            if (TrySetPriorityValueOnMember(cameraComponent, priorityField.FieldType, priorityField.GetValue(cameraComponent), priority, out object updatedFieldValue))
            {
                priorityField.SetValue(cameraComponent, updatedFieldValue);
                return;
            }
        }

        var directPriorityField = type.GetField("Priority");
        if (directPriorityField != null)
        {
            if (TrySetPriorityValueOnMember(cameraComponent, directPriorityField.FieldType, directPriorityField.GetValue(cameraComponent), priority, out object updatedDirectFieldValue))
            {
                directPriorityField.SetValue(cameraComponent, updatedDirectFieldValue);
            }
        }
    }

    private static bool TrySetPriorityValueOnMember(object owner, System.Type memberType, object currentValue, int priority, out object updatedValue)
    {
        updatedValue = currentValue;

        if (memberType == typeof(int))
        {
            updatedValue = priority;
            return true;
        }

        // Soporta wrappers tipo PrioritySettings (Cinemachine 3.x): campo/propiedad "Value".
        if (currentValue == null)
        {
            return false;
        }

        var wrappedType = currentValue.GetType();

        var valueProp = wrappedType.GetProperty("Value");
        if (valueProp != null && valueProp.CanWrite && valueProp.PropertyType == typeof(int))
        {
            valueProp.SetValue(currentValue, priority);
            updatedValue = currentValue;
            return true;
        }

        var valueField = wrappedType.GetField("Value");
        if (valueField != null && valueField.FieldType == typeof(int))
        {
            valueField.SetValue(currentValue, priority);
            updatedValue = currentValue;
            return true;
        }

        return false;
    }
}
