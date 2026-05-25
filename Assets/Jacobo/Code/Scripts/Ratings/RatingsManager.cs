using System.Collections;
using UnityEngine;

public class RatingsManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RatingsCalculator ratingsCalculator;
    [SerializeField] private RatingsUI ratingsUI;
    [SerializeField] private AudioLibrary audioLibrary;
    [SerializeField] private SoundManager soundManager;
    [SerializeField] private AnswerHandler answerHandler;
    [SerializeField] private CinematicManager cinematicManager;
    [SerializeField] private GameObject ratingsPanelRoot;
    [Tooltip("Opcional. Si está asignado, se detiene la música de pregunta/cues al iniciar el flow de ratings.")]
    [SerializeField] private TimerFeedbackController timerFeedbackController;

    [Header("Timing")]
    [Tooltip("(Legacy) Delay usado antes de PlayNext. Se mantiene por compatibilidad; si 'leaderboardAudioEndDelay' es 0, se usa este valor.")]
    [SerializeField] private float postAnimationDelay = 2f;
    [SerializeField] private float showFromZeroDelay = 0.75f;
    [Tooltip("(Legacy) Delay usado después del audio de rating. Se mantiene por compatibilidad; si 'leaderboardAudioEndDelay' es 0, se usa este valor.")]
    [SerializeField] private float postRatingAudioDelay = 0.5f;

    [Header("Leaderboard Audio")]
    [Tooltip("Delay opcional ANTES de reproducir el audio de leaderboard (state/hitcap).")]
    [SerializeField] private float leaderboardAudioStartDelay = 0f;

    [Tooltip("Delay opcional DESPUÉS del audio de leaderboard (state/hitcap) antes de ocultar el panel y hacer PlayNext.\nSi es 0, se usa el delay legacy (postRatingAudioDelay / postAnimationDelay).")]
    [SerializeField] private float leaderboardAudioEndDelay = 0f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private Coroutine flowRoutine;

    private void Awake()
    {
        if (timerFeedbackController == null)
        {
            timerFeedbackController = FindFirstObjectByType<TimerFeedbackController>();
        }
    }

    public void OnAnswerRegistered(bool isCorrect)
    {
        if (flowRoutine != null)
        {
            StopCoroutine(flowRoutine);
        }

        flowRoutine = StartCoroutine(HandleAnswerFlow(isCorrect));
    }

    public void ShowCurrentFromZero()
    {
        if (flowRoutine != null)
        {
            StopCoroutine(flowRoutine);
        }

        flowRoutine = StartCoroutine(HandleShowCurrentFromZero());
    }

    public void OnHoldCompletePlayRatingsFlow()
    {
        if (flowRoutine != null)
        {
            StopCoroutine(flowRoutine);
        }

        flowRoutine = StartCoroutine(HandleHoldCompleteFlow());
    }

    private IEnumerator HandleAnswerFlow(bool isCorrect)
    {
        timerFeedbackController?.OnTimerStopped();

        if (ratingsCalculator == null)
        {
            if (debugLogs)
            {
                Debug.LogWarning($"{nameof(RatingsManager)}: Missing RatingsCalculator.");
            }
            yield break;
        }

        RatingResult result = ratingsCalculator.ApplyAnswerResult(isCorrect);

        if (ratingsPanelRoot != null)
        {
            ratingsPanelRoot.SetActive(true);
        }

        if (ratingsUI != null)
        {
            yield return StartCoroutine(ratingsUI.AnimateRatings(result, audioLibrary));
        }

        if (leaderboardAudioStartDelay > 0f)
        {
            yield return new WaitForSeconds(leaderboardAudioStartDelay);
        }

        float ratingAudioDuration = PlayLeaderboardAudioAndGetDuration(result);
        if (ratingAudioDuration > 0f)
        {
            yield return new WaitForSeconds(ratingAudioDuration);
        }

        float endDelay = ResolveLeaderboardEndDelay();
        if (endDelay > 0f)
        {
            yield return new WaitForSeconds(endDelay);
        }

        if (ratingsPanelRoot != null)
        {
            ratingsPanelRoot.SetActive(false);
        }

        cinematicManager?.PlayNext();

        flowRoutine = null;
    }

    private IEnumerator HandleShowCurrentFromZero()
    {
        if (ratingsCalculator == null || ratingsUI == null)
        {
            if (debugLogs)
            {
                Debug.LogWarning($"{nameof(RatingsManager)}: Missing RatingsCalculator or RatingsUI.");
            }
            flowRoutine = null;
            yield break;
        }

        if (ratingsPanelRoot != null)
        {
            ratingsPanelRoot.SetActive(true);
        }

        ratingsUI.SetToZeroState();

        if (showFromZeroDelay > 0f)
        {
            yield return new WaitForSeconds(showFromZeroDelay);
        }

        RatingResult result = ratingsCalculator.GetCurrentResult();
        yield return StartCoroutine(ratingsUI.AnimateFromZeroToCurrent(result, audioLibrary));

        flowRoutine = null;
    }

    private IEnumerator HandleHoldCompleteFlow()
    {
        timerFeedbackController?.OnTimerStopped();

        if (ratingsCalculator == null)
        {
            if (debugLogs)
            {
                Debug.LogWarning($"{nameof(RatingsManager)}: Missing RatingsCalculator.");
            }
            flowRoutine = null;
            yield break;
        }

        AnswerHandler.AnswerOutcome outcome = AnswerHandler.AnswerOutcome.Wrong;
        if (answerHandler != null)
        {
            outcome = answerHandler.LastOutcome;
        }

        bool isCorrect = outcome == AnswerHandler.AnswerOutcome.Correct;
        RatingResult result = ratingsCalculator.ApplyAnswerResult(isCorrect);

        if (ratingsPanelRoot != null)
        {
            ratingsPanelRoot.SetActive(true);
        }

        if (ratingsUI != null)
        {
            yield return StartCoroutine(ratingsUI.AnimateRatings(result, audioLibrary));
        }

        if (leaderboardAudioStartDelay > 0f)
        {
            yield return new WaitForSeconds(leaderboardAudioStartDelay);
        }

        float ratingAudioDuration = PlayLeaderboardAudioAndGetDuration(result);
        if (ratingAudioDuration > 0f)
        {
            yield return new WaitForSeconds(ratingAudioDuration);
        }

        float endDelay = ResolveLeaderboardEndDelay();
        if (endDelay > 0f)
        {
            yield return new WaitForSeconds(endDelay);
        }

        if (ratingsPanelRoot != null)
        {
            ratingsPanelRoot.SetActive(false);
        }

        cinematicManager?.PlayNext();

        flowRoutine = null;
    }

    private float PlayLeaderboardAudioAndGetDuration(RatingResult result)
    {
        if (audioLibrary == null)
        {
            return 0f;
        }

        if (result.HitCap)
        {
            return result.PlayerRating <= 0
                ? audioLibrary.PlayStayAtBottomWithDuration()
                : audioLibrary.PlayStayAtTopWithDuration();
        }

        return audioLibrary.PlayStateWithDuration(result.PlayerState);
    }

    private float ResolveLeaderboardEndDelay()
    {
        if (leaderboardAudioEndDelay > 0f)
        {
            return leaderboardAudioEndDelay;
        }

        // Compatibilidad con escenas ya configuradas.
        return Mathf.Max(0f, Mathf.Max(postRatingAudioDelay, postAnimationDelay));
    }
}
