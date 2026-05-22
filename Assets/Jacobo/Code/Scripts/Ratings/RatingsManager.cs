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

    [Header("Timing")]
    [SerializeField] private float postAnimationDelay = 2f;
    [SerializeField] private float showFromZeroDelay = 0.75f;
    [SerializeField] private float postRatingAudioDelay = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private Coroutine flowRoutine;

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
        SoundManager resolvedSoundManager = soundManager != null ? soundManager : SoundManager.Instance;
        if (resolvedSoundManager != null)
        {
            if (isCorrect)
            {
                resolvedSoundManager.PlayPositiveFeedback();
            }
            else
            {
                resolvedSoundManager.PlayNegativeFeedback();
            }
        }

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

        if (audioLibrary != null)
        {
            if (result.HitCap)
            {
                if (result.PlayerRating <= 0)
                {
                    audioLibrary.PlayStayAtBottom();
                }
                else
                {
                    audioLibrary.PlayStayAtTop();
                }
            }
            else
            {
                audioLibrary.PlayState(result.PlayerState);
            }
        }

        if (postAnimationDelay > 0f)
        {
            yield return new WaitForSeconds(postAnimationDelay);
        }

        if (cinematicManager != null)
        {
            cinematicManager.PlayNext();
        }

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

        SoundManager resolvedSoundManager = soundManager != null ? soundManager : SoundManager.Instance;
        if (resolvedSoundManager != null)
        {
            if (outcome == AnswerHandler.AnswerOutcome.Correct)
            {
                resolvedSoundManager.PlayPositiveFeedback();
            }
            else if (outcome == AnswerHandler.AnswerOutcome.NoAnswer)
            {
                resolvedSoundManager.PlayNoPoseFeedback();
            }
            else
            {
                resolvedSoundManager.PlayNegativeFeedback();
            }
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

        float ratingAudioDuration = 0f;
        if (audioLibrary != null)
        {
            if (result.HitCap)
            {
                if (result.PlayerRating <= 0)
                {
                    ratingAudioDuration = audioLibrary.PlayStayAtBottomWithDuration();
                }
                else
                {
                    ratingAudioDuration = audioLibrary.PlayStayAtTopWithDuration();
                }
            }
            else
            {
                ratingAudioDuration = audioLibrary.PlayStateWithDuration(result.PlayerState);
            }
        }

        if (ratingAudioDuration > 0f)
        {
            yield return new WaitForSeconds(ratingAudioDuration);
        }

        if (postRatingAudioDelay > 0f)
        {
            yield return new WaitForSeconds(postRatingAudioDelay);
        }

        if (ratingsPanelRoot != null)
        {
            ratingsPanelRoot.SetActive(false);
        }

        if (cinematicManager != null)
        {
            cinematicManager.PlayNext();
        }

        flowRoutine = null;
    }
}
