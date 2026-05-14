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

        bool panelIsActive = questionPanel.activeSelf;

        // Detecta cuando el panel se activa
        if (panelIsActive && !panelWasActive)
        {
            RestartTimer();
        }

        // Detecta cuando el panel se desactiva
        if (!panelIsActive)
        {
            timerRunning = false;
        }

        panelWasActive = panelIsActive;

        // Si el timer no está corriendo, salir
        if (!timerRunning)
            return;

        timer -= Time.deltaTime;

        // Se acabó el tiempo
        if (timer <= 0f && !answerSubmitted)
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

        Debug.Log("Timer reiniciado");
    }

    void SubmitRandomAnswer()
    {
        // Tus interacciones tienen 3 respuestas
        int randomOption = Random.Range(0, 3);

        // Busca SOLO el InteractionActionBase activo
        InteractionActionBase[] interactions =
            FindObjectsByType<InteractionActionBase>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        InteractionActionBase activeInteraction = null;

        foreach (var interaction in interactions)
        {
            if (interaction.isActiveAndEnabled &&
                interaction.gameObject.activeInHierarchy)
            {
                activeInteraction = interaction;
                break;
            }
        }

        if (activeInteraction == null)
        {
            Debug.LogWarning("No se encontró ninguna interacción activa.");
            return;
        }

        InteractionType activeType = activeInteraction.InteractionType;

        Debug.Log(
            $"Tiempo agotado. Interacción activa: {activeType} | " +
            $"Respuesta automática: {randomOption}"
        );

        answerHandler.SubmitAnswer(activeType, randomOption);

        answerSubmitted = true;
    }

    void UpdateTimerText()
    {
        int minutes = Mathf.FloorToInt(timer / 60);
        int seconds = Mathf.CeilToInt(timer % 60);

        // Evita que aparezca 00:60
        if (seconds == 60)
        {
            minutes++;
            seconds = 0;
        }

        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}