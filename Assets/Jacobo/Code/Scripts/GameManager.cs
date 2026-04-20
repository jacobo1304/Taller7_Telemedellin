using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private InteractionUIManager uiManager;
    [SerializeField] private AnswerHandler answerHandler;
    [SerializeField] private CustomPoseDetector customPoseDetector;

    [Header("Flow")]
    [SerializeField] private List<InteractionActionBase> interactionOrder = new List<InteractionActionBase>();
    [SerializeField] private int startIndex = 0;
    [SerializeField] private bool autoAdvanceOnCorrect = true;
    [SerializeField, Min(0f)] private float extraDelayAfterFeedback = 0f;
    [SerializeField] private bool onlyCurrentInteractionActive = true;

    private int currentIndex = -1;
    private int queuedNextIndex = -1;
    private Coroutine delayedAdvanceRoutine;

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

        answerHandler?.SetInputLocked(false);
        customPoseDetector?.SetResponseLock(false);
        SetCurrentInteraction(startIndex);
    }

    private void OnDisable()
    {
        if (answerHandler != null)
        {
            answerHandler.onAnswerCorrect.RemoveListener(HandleCorrectAnswer);
            answerHandler.onAnswerWrong.RemoveListener(HandleWrongAnswer);
        }

        if (delayedAdvanceRoutine != null)
        {
            StopCoroutine(delayedAdvanceRoutine);
            delayedAdvanceRoutine = null;
        }
    }

    public void NextInteraction()
    {
        SetCurrentInteraction(currentIndex + 1);
    }

    public void PreviousInteraction()
    {
        SetCurrentInteraction(currentIndex - 1);
    }

    // Método público para futuro uso desde Director/Timeline.
    // Si hay una interacción en cola, avanza a esa; si no, avanza a la siguiente inmediata.
    public void ContinueAfterCinematic()
    {
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
}
