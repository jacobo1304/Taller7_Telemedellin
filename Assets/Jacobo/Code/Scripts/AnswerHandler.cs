using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;

public class AnswerHandler : MonoBehaviour
{
    [Serializable]
    public class AnswerEvent : UnityEvent<InteractionType> { }

    [Serializable]
    public class InteractionBinding
    {
        public InteractionType interactionType;
        public InteractionActionBase interactionAction;
    }

    [Header("Interaction Routing")]
    [SerializeField] private List<InteractionBinding> interactions = new List<InteractionBinding>();
    [SerializeField] private InteractionUIManager uiManager;

    [Header("Global Events")]
    public AnswerEvent onAnswerCorrect;
    public AnswerEvent onAnswerWrong;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;
    [SerializeField] private bool inputLocked = false;

    private readonly Dictionary<InteractionType, InteractionActionBase>
        actionByInteractionType =
            new Dictionary<InteractionType, InteractionActionBase>();

    // NUEVO: interacción actual
    private InteractionType currentInteractionType;
    private bool hasCurrentInteraction = false;

    public bool HasCurrentInteraction => hasCurrentInteraction;
    public InteractionType CurrentInteractionType => currentInteractionType;

    public void SetCurrentInteraction(
        InteractionType interactionType
    )
    {
        currentInteractionType = interactionType;
        hasCurrentInteraction = true;

        if (debugLogs)
        {
            Debug.Log(
                $"Interacción actual: {interactionType}",
                this
            );
        }
    }

    public void SetInputLocked(bool locked)
    {
        inputLocked = locked;

        if (debugLogs)
        {
            Debug.Log(
                $"{nameof(AnswerHandler)}: inputLocked={(inputLocked ? "TRUE" : "FALSE")}",
                this
            );
        }
    }

    private void Awake()
    {
        BuildLookup();

        if (uiManager == null)
        {
            uiManager =
                FindFirstObjectByType<InteractionUIManager>();
        }
    }

    private void BuildLookup()
    {
        actionByInteractionType.Clear();

        for (int i = 0; i < interactions.Count; i++)
        {
            var item = interactions[i];

            if (item == null ||
                item.interactionAction == null)
            {
                if (debugLogs)
                {
                    Debug.LogWarning(
                        $"{nameof(AnswerHandler)}: Interaction binding inválido en índice {i}.",
                        this
                    );
                }

                continue;
            }

            InteractionType key =
                item.interactionType;

            if (actionByInteractionType.ContainsKey(key))
            {
                if (debugLogs)
                {
                    Debug.LogWarning(
                        $"{nameof(AnswerHandler)}: interactionType duplicado '{key}'. Se conserva el primero.",
                        this
                    );
                }

                continue;
            }

            actionByInteractionType.Add(
                key,
                item.interactionAction
            );
        }
    }

    public void SubmitAnswer(
        InteractionType interactionType,
        int selectedOptionIndex
    )
    {
        if (inputLocked)
        {
            if (debugLogs)
            {
                Debug.Log(
                    $"{nameof(AnswerHandler)}: Input bloqueado. Ignorando respuesta para '{interactionType}' opción {selectedOptionIndex}.",
                    this
                );
            }

            return;
        }

        if (!actionByInteractionType.TryGetValue(
            interactionType,
            out var action))
        {
            if (debugLogs)
            {
                Debug.LogWarning(
                    $"{nameof(AnswerHandler)}: No se encontró interacción para '{interactionType}'.",
                    this
                );
            }

            return;
        }

        bool isCorrect =
            action.HandleAnswer(
                selectedOptionIndex,
                uiManager
            );

        if (isCorrect)
        {
            if (debugLogs)
            {
                Debug.Log(
                    $"Respuesta correcta para la pregunta de tipo {interactionType} seleccionaste la opcion {selectedOptionIndex}.",
                    this
                );
            }

            onAnswerCorrect?.Invoke(
                interactionType
            );
        }
        else
        {
            onAnswerWrong?.Invoke(
                interactionType
            );
        }

        if (debugLogs)
        {
            Debug.Log(
                $"{nameof(AnswerHandler)}: '{interactionType}' opción {selectedOptionIndex} => {(isCorrect ? "CORRECTA" : "INCORRECTA")}",
                this
            );
        }
    }

    public void SubmitAnswer(
        InteractionType interactionType,
        bool isCorrect
    )
    {
        if (!actionByInteractionType.TryGetValue(
            interactionType,
            out var action))
        {
            if (debugLogs)
            {
                Debug.LogWarning(
                    $"{nameof(AnswerHandler)}: No se encontró interacción para '{interactionType}'.",
                    this
                );
            }

            if (isCorrect)
            {
                onAnswerCorrect?.Invoke(
                    interactionType
                );
            }
            else
            {
                onAnswerWrong?.Invoke(
                    interactionType
                );
            }

            return;
        }

        int fallbackOption =
            isCorrect
                ? action.CorrectOptionIndex
                : action.WrongOption1Index;

        SubmitAnswer(
            interactionType,
            fallbackOption
        );
    }

    public void SubmitAnswer(
        string interactionTypeName,
        int selectedOptionIndex
    )
    {
        if (!Enum.TryParse(
            interactionTypeName,
            true,
            out InteractionType parsedType))
        {
            if (debugLogs)
            {
                Debug.LogWarning(
                    $"{nameof(AnswerHandler)}: interactionType inválido '{interactionTypeName}'.",
                    this
                );
            }

            return;
        }

        SubmitAnswer(
            parsedType,
            selectedOptionIndex
        );
    }

    public void SubmitAnswer(
        string interactionTypeName,
        bool isCorrect
    )
    {
        if (!Enum.TryParse(
            interactionTypeName,
            true,
            out InteractionType parsedType))
        {
            if (debugLogs)
            {
                Debug.LogWarning(
                    $"{nameof(AnswerHandler)}: interactionType inválido '{interactionTypeName}'.",
                    this
                );
            }

            return;
        }

        SubmitAnswer(parsedType, isCorrect);
    }

    public void PreviewSelection(
        InteractionType interactionType,
        int selectedOptionIndex
    )
    {
        if (!actionByInteractionType.TryGetValue(
            interactionType,
            out var action))
        {
            return;
        }

        action.PreviewOption(
            selectedOptionIndex
        );
    }

    public void ClearPreview(
        InteractionType interactionType
    )
    {
        if (!actionByInteractionType.TryGetValue(
            interactionType,
            out var action))
        {
            return;
        }

        action.ResetHoldEffects();
    }

    public void ClearAllPreviews()
    {
        foreach (var pair
            in actionByInteractionType)
        {
            pair.Value?.ResetHoldEffects();
        }
    }
}