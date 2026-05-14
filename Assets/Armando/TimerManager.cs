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

    private float timer;
    private bool timerRunning = false;
    private bool panelWasActive = false;
    private bool answerSubmitted = false;

    private void Start()
    {
        timer = startTime;
        UpdateTimerText();
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
        }

        panelWasActive = panelIsActive;

        if (!timerRunning)
            return;

        timer -= Time.deltaTime;

        if (timer <= 0f &&
            !answerSubmitted)
        {
            timer = 0f;
            timerRunning = false;

            SubmitRandomAnswer();
        }

        UpdateTimerText();
    }

    void RestartTimer()
    {
        timer = startTime;
        timerRunning = true;
        answerSubmitted = false;

        UpdateTimerText();

        Debug.Log(
            "Timer reiniciado"
        );
    }

    void SubmitRandomAnswer()
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

        int randomOption =
            Random.Range(0, 3);

        InteractionType currentType =
            answerHandler
                .CurrentInteractionType;

        Debug.Log(
            $"Tiempo agotado | " +
            $"Interacción: {currentType} | " +
            $"Respuesta automática: {randomOption}"
        );

        answerHandler.SubmitAnswer(
            currentType,
            randomOption
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

        timerText.text =
            string.Format(
                "{0:00}:{1:00}",
                minutes,
                seconds
            );
    }
}