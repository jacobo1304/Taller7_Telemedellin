using TMPro;
using UnityEngine;

public class TimerManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text timerText;
    public GameObject questionPanel;

    [Header("Timer Settings")]
    public float startTime = 30f;

    [Header("Answer System")]
    public AnswerHandler answerHandler;

    [Header("Feedback")]
    [SerializeField] private TimerFeedbackController feedbackController;

    private float timer;
    private bool timerRunning = false;
    private bool panelWasActive = false;
    private bool answerSubmitted = false;

    private void Start()
    {
        timer = startTime;
        UpdateTimerText();

        if (feedbackController == null)
        {
            feedbackController = GetComponentInChildren<TimerFeedbackController>(true);
        }
    }

    private void Update()
    {
        if (questionPanel == null)
            return;

        bool panelIsActive =
            questionPanel.activeSelf;

        if (panelIsActive &&
            !panelWasActive)
        {
            RestartTimer();
        }

        if (!panelIsActive)
        {
            timerRunning = false;
            feedbackController?.OnTimerStopped();
        }

        panelWasActive = panelIsActive;

        if (!timerRunning)
            return;

        timer -= Time.deltaTime;

        feedbackController?.OnTick(timer, startTime);

        if (timer <= 0f &&
            !answerSubmitted)
        {
            timer = 0f;
            timerRunning = false;

            feedbackController?.OnTimeout();
            SubmitDefaultAnswer();
        }

        UpdateTimerText();
    }

    void RestartTimer()
    {
        timer = startTime;
        timerRunning = true;
        answerSubmitted = false;

        feedbackController?.OnTimerStart(startTime);

        UpdateTimerText();

        Debug.Log(
            "Timer reiniciado"
        );
    }

    void SubmitDefaultAnswer()
    {
        if (answerHandler == null)
        {
            Debug.LogWarning(
                "AnswerHandler no asignado."
            );
            return;
        }

        if (!answerHandler
            .HasCurrentInteraction)
        {
            Debug.LogWarning(
                "No hay interacción actual."
            );
            return;
        }

        InteractionType currentType =
            answerHandler
                .CurrentInteractionType;

        Debug.Log(
            $"Tiempo agotado | " +
            $"Interacción: {currentType} | " +
            "Respuesta automática: default timeout"
        );

        answerHandler.SubmitDefaultAnswer(
            currentType
        );

        answerSubmitted = true;
    }

    void UpdateTimerText()
    {
        int minutes =
            Mathf.FloorToInt(timer / 60);

        int seconds =
            Mathf.CeilToInt(timer % 60);

        if (seconds == 60)
        {
            minutes++;
            seconds = 0;
        }

        if (timerText != null)
        {
            timerText.text =
                string.Format(
                    "{0:00}:{1:00}",
                    minutes,
                    seconds
                );
        }
    }

}