using System.Collections;
using UnityEngine;

public class RatingsManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RatingsCalculator ratingsCalculator;
    [SerializeField] private RatingsUI ratingsUI;
    [SerializeField] private AudioLibrary audioLibrary;
    [SerializeField] private SoundManager soundManager;
    [SerializeField] private CinematicManager cinematicManager;
    [SerializeField] private GameObject ratingsPanelRoot;

    [Header("Timing")]
    [SerializeField] private float postAnimationDelay = 2f;

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
}
