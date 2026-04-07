using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private InteractionUIManager uiManager;
    [SerializeField] private AnswerHandler answerHandler;

    [Header("Flow")]
    [SerializeField] private List<InteractionActionBase> interactionOrder = new List<InteractionActionBase>();
    [SerializeField] private int startIndex = 0;
    [SerializeField] private bool autoAdvanceOnCorrect = true;
    [SerializeField] private bool onlyCurrentInteractionActive = true;

    private int currentIndex = -1;

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
    }

    private void OnEnable()
    {
        if (answerHandler != null)
        {
            answerHandler.onAnswerCorrect.AddListener(HandleCorrectAnswer);
        }
    }

    private void Start()
    {
        if (interactionOrder.Count == 0)
        {
            Debug.LogWarning($"{nameof(GameManager)}: No hay interacciones configuradas en 'interactionOrder'.", this);
            return;
        }

        SetCurrentInteraction(startIndex);
    }

    private void OnDisable()
    {
        if (answerHandler != null)
        {
            answerHandler.onAnswerCorrect.RemoveListener(HandleCorrectAnswer);
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

    public void SetCurrentInteraction(int index)
    {
        if (interactionOrder.Count == 0)
        {
            return;
        }

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

        current.PresentToUI(uiManager);
        uiManager?.ClearHoldProgress();
    }

    private void HandleCorrectAnswer(InteractionType type)
    {
        if (!autoAdvanceOnCorrect)
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
            SetCurrentInteraction(currentIndex + 1);
        }
    }
}
