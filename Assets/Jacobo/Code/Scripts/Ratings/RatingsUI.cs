using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RatingsUI : MonoBehaviour
{
    private const int MinRating = 0;
    private const int MaxRating = 10000;

    [Header("Bars")]
    [SerializeField] private Image playerBar;
    [SerializeField] private Image[] competitorBars = new Image[3];

    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text playerRatingText;
    [SerializeField] private RectTransform playerRatingTextTransform;
    [SerializeField] private float playerTextMinY = 0f;
    [SerializeField] private float playerTextMaxY = 10000f;

    [Header("Animation")]
    [SerializeField] private float animateDuration = 2.5f;

    [Header("Audio")]
    [Tooltip("Cada cuántos puntos de rating suena el tick durante la animación.\nEj: 10 => suena cada 10 puntos.")]
    [SerializeField, Min(1)] private int tickStep = 10;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    public IEnumerator AnimateRatings(RatingResult result, AudioLibrary audioLib)
    {
        int startPlayer = GetBarValue(playerBar);
        int[] startCompetitors = GetCompetitorValues();
        yield return AnimateInternal(startPlayer, startCompetitors, result, audioLib);
    }

    public IEnumerator AnimateFromZeroToCurrent(RatingResult result, AudioLibrary audioLib)
    {
        int[] zeros = new int[3];
        yield return AnimateInternal(0, zeros, result, audioLib);
    }

    private IEnumerator AnimateInternal(int startPlayer, int[] startCompetitors, RatingResult result, AudioLibrary audioLib)
    {
        int[] startComp = NormalizeCompetitorArray(startCompetitors);
        int[] targetComp = NormalizeCompetitorArray(result.CompetitorRatings);

        startPlayer = Mathf.Clamp(startPlayer, MinRating, MaxRating);
        int targetPlayer = Mathf.Clamp(result.PlayerRating, MinRating, MaxRating);

        SetBars(startPlayer, startComp);
        SetPlayerNumber(startPlayer);

        float duration = Mathf.Max(0.01f, animateDuration);
        float t = 0f;

        // Tick pacing: garantiza que haya ticks durante TODA la animación mientras el rating cambie,
        // sin depender de redondeos del número (que pueden "pegarse" al final).
        int ratingDiff = Mathf.Abs(targetPlayer - startPlayer);
        int step = Mathf.Max(1, tickStep);
        int desiredTicks = ratingDiff > 0 ? Mathf.Max(1, ratingDiff / step) : 0;

        float tickClipLength = audioLib != null ? audioLib.GetTickClipLength() : 0f;
        float minTickInterval = tickClipLength > 0f ? Mathf.Clamp(tickClipLength * 0.9f, 0.05f, 0.25f) : 0.07f;
        float maxTickInterval = Mathf.Max(minTickInterval, 0.25f);
        float computedInterval = desiredTicks > 0 ? (duration / desiredTicks) : duration;
        float tickInterval = Mathf.Clamp(computedInterval, minTickInterval, maxTickInterval);

        // Primer tick al iniciar la subida.
        if (desiredTicks > 0 && audioLib != null)
        {
            audioLib.PlayTick();
        }

        float nextTickAt = tickInterval;

        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / duration);

            int playerValue = Mathf.RoundToInt(Mathf.Lerp(startPlayer, targetPlayer, lerp));
            int[] compValues = new int[3];
            for (int i = 0; i < compValues.Length; i++)
            {
                compValues[i] = Mathf.RoundToInt(Mathf.Lerp(startComp[i], targetComp[i], lerp));
            }

            SetBars(playerValue, compValues);
            SetPlayerNumber(playerValue);

            if (desiredTicks > 0 && audioLib != null && t >= nextTickAt)
            {
                audioLib.PlayTick();
                nextTickAt += tickInterval;
            }

            yield return null;
        }

        SetBars(targetPlayer, targetComp);
        SetPlayerNumber(targetPlayer);
        SetTitle(result.PlayerState);

        if (debugLogs)
        {
            Debug.Log($"{nameof(RatingsUI)}: Animation complete. Player={targetPlayer} State={result.PlayerState}.");
        }
    }

    private void SetBars(int playerValue, int[] competitorValues)
    {
        if (playerBar != null)
        {
            playerBar.fillAmount = Mathf.Clamp01(playerValue / 10000f);
        }

        if (competitorBars == null)
        {
            return;
        }

        int[] normalized = NormalizeCompetitorArray(competitorValues);
        for (int i = 0; i < competitorBars.Length; i++)
        {
            Image bar = competitorBars[i];
            if (bar == null)
            {
                continue;
            }

            bar.fillAmount = Mathf.Clamp01(normalized[i] / 10000f);
        }
    }

    private void SetPlayerNumber(int value)
    {
        if (playerRatingText != null)
        {
            playerRatingText.text = value.ToString();
        }

        if (titleText != null)
        {
            titleText.text = value.ToString();
        }

        UpdatePlayerTextPosition(value);
    }

    public void SetToZeroState()
    {
        int[] zeros = new int[3];
        SetBars(0, zeros);
        SetPlayerNumber(0);
    }

    private void UpdatePlayerTextPosition(int value)
    {
        if (playerRatingTextTransform == null && playerRatingText != null)
        {
            playerRatingTextTransform = playerRatingText.rectTransform;
        }

        if (playerRatingTextTransform == null)
        {
            return;
        }

        float t = Mathf.Clamp01(value / 10000f);
        float y = Mathf.Lerp(playerTextMinY, playerTextMaxY, t);
        Vector2 pos = playerRatingTextTransform.anchoredPosition;
        pos.y = y;
        playerRatingTextTransform.anchoredPosition = pos;
    }

    private void SetTitle(RatingState state)
    {
        if (titleText == null)
        {
            return;
        }

        titleText.text = GetStateTitle(state);
    }

    private static string GetStateTitle(RatingState state)
    {
        switch (state)
        {
            case RatingState.MuyMal:
                return "Muy mal";
            case RatingState.Mal:
                return "Mal";
            case RatingState.Promedio:
                return "Promedio";
            case RatingState.Bien:
                return "Bien";
            case RatingState.MuyBien:
                return "Muy bien";
            default:
                return string.Empty;
        }
    }

    private int GetBarValue(Image bar)
    {
        if (bar == null)
        {
            return 0;
        }

        return Mathf.RoundToInt(Mathf.Clamp01(bar.fillAmount) * MaxRating);
    }

    private int[] GetCompetitorValues()
    {
        int[] values = new int[3];
        if (competitorBars == null)
        {
            return values;
        }

        for (int i = 0; i < values.Length && i < competitorBars.Length; i++)
        {
            values[i] = GetBarValue(competitorBars[i]);
        }

        return values;
    }

    private static int[] NormalizeCompetitorArray(int[] competitorValues)
    {
        int[] normalized = new int[3];
        if (competitorValues == null)
        {
            return normalized;
        }

        for (int i = 0; i < normalized.Length && i < competitorValues.Length; i++)
        {
            normalized[i] = Mathf.Clamp(competitorValues[i], MinRating, MaxRating);
        }

        return normalized;
    }
}
