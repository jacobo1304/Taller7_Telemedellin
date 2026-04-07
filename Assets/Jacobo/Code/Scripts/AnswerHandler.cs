using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;

public class AnswerHandler : MonoBehaviour
{
    [Serializable]
    public class AnswerEvent : UnityEvent<string> { }

    [Serializable]
    public class InteractionBinding
    {
        public string interactionId;
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

    private readonly Dictionary<string, InteractionActionBase> actionByInteractionId = new Dictionary<string, InteractionActionBase>();

    private void Awake()
    {
        BuildLookup();

        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<InteractionUIManager>();
        }
    }

    private void BuildLookup()
    {
        actionByInteractionId.Clear();

        for (int i = 0; i < interactions.Count; i++)
        {
            var item = interactions[i];
            if (item == null || string.IsNullOrWhiteSpace(item.interactionId) || item.interactionAction == null)
            {
                if (debugLogs)
                {
                    Debug.LogWarning($"{nameof(AnswerHandler)}: Interaction binding inválido en índice {i}.", this);
                }
                continue;
            }

            string key = item.interactionId.Trim();
            if (actionByInteractionId.ContainsKey(key))
            {
                if (debugLogs)
                {
                    Debug.LogWarning($"{nameof(AnswerHandler)}: interactionId duplicado '{key}'. Se conserva el primero.", this);
                }
                continue;
            }

            actionByInteractionId.Add(key, item.interactionAction);
        }
    }

    // Nuevo flujo: recibe cuál opción (0,1,2) eligió el usuario.
    public void SubmitAnswer(string interactionId, int selectedOptionIndex)
    {
        if (string.IsNullOrWhiteSpace(interactionId))
        {
            if (debugLogs)
            {
                Debug.LogWarning($"{nameof(AnswerHandler)}: interactionId vacío.", this);
            }
            return;
        }

        string key = interactionId.Trim();
        if (!actionByInteractionId.TryGetValue(key, out var action))
        {
            if (debugLogs)
            {
                Debug.LogWarning($"{nameof(AnswerHandler)}: No se encontró interacción para '{key}'.", this);
            }
            return;
        }

        bool isCorrect = action.HandleAnswer(selectedOptionIndex, uiManager);

        if (isCorrect)
        {
            onAnswerCorrect?.Invoke(key);
        }
        else
        {
            onAnswerWrong?.Invoke(key);
        }

        if (debugLogs)
        {
            Debug.Log($"{nameof(AnswerHandler)}: '{key}' opción {selectedOptionIndex} => {(isCorrect ? "CORRECTA" : "INCORRECTA")}", this);
        }
    }

    // Compatibilidad con el detector de poses existente (bool).
    public void SubmitAnswer(string interactionId, bool isCorrect)
    {
        if (string.IsNullOrWhiteSpace(interactionId))
        {
            if (debugLogs)
            {
                Debug.LogWarning($"{nameof(AnswerHandler)}: interactionId vacío.", this);
            }
            return;
        }

        string key = interactionId.Trim();
        if (!actionByInteractionId.TryGetValue(key, out var action))
        {
            if (debugLogs)
            {
                Debug.LogWarning($"{nameof(AnswerHandler)}: No se encontró interacción para '{key}'.", this);
            }

            // Si no hay acción enrutable, al menos se reportan los eventos globales.
            if (isCorrect) onAnswerCorrect?.Invoke(key);
            else onAnswerWrong?.Invoke(key);
            return;
        }

        int fallbackOption = isCorrect ? action.CorrectOptionIndex : action.WrongOption1Index;
        SubmitAnswer(key, fallbackOption);
    }
}
