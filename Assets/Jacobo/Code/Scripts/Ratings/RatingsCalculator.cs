using UnityEngine;

public class RatingsCalculator : MonoBehaviour
{
    private const int MinRating = 0;
    private const int MaxRating = 10000;
    private const int RatingDelta = 2500;
    private const int MinCompetitorDelta = 101;

    [Header("Ratings")]
    [SerializeField] private int playerRating = 5000;
    [SerializeField] private int[] competitorRatings = new int[3] { 5000, 5000, 5000 };

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    public int PlayerRating => playerRating;
    public int[] CompetitorRatings => competitorRatings;

    public RatingResult GetCurrentResult()
    {
        EnsureCompetitorArray();

        int[] competitorSnapshot = new int[competitorRatings.Length];
        System.Array.Copy(competitorRatings, competitorSnapshot, competitorRatings.Length);
        RatingState playerState = ResolveState(playerRating);
        bool hitCap = playerRating == MinRating || playerRating == MaxRating;

        return new RatingResult(playerRating, competitorSnapshot, playerState, hitCap);
    }

    private void OnValidate()
    {
        EnsureCompetitorArray();
        playerRating = ClampRating(playerRating);

        for (int i = 0; i < competitorRatings.Length; i++)
        {
            competitorRatings[i] = ClampRating(competitorRatings[i]);
        }
    }

    public RatingResult ApplyAnswerResult(bool isCorrect)
    {
        EnsureCompetitorArray();

        int before = playerRating;
        int delta = isCorrect ? RatingDelta : -RatingDelta;
        int after = ClampRating(before + delta);
        bool hitCap = (before == MinRating || before == MaxRating) && after == before;

        playerRating = after;

        int[] distribution = GenerateDistribution(RatingDelta, MinCompetitorDelta, competitorRatings.Length);
        for (int i = 0; i < competitorRatings.Length; i++)
        {
            int competitorDelta = isCorrect ? -distribution[i] : distribution[i];
            competitorRatings[i] = ClampRating(competitorRatings[i] + competitorDelta);
        }

        RatingState playerState = ResolveState(playerRating);
        int[] competitorSnapshot = new int[competitorRatings.Length];
        System.Array.Copy(competitorRatings, competitorSnapshot, competitorRatings.Length);

        if (debugLogs)
        {
            Debug.Log($"{nameof(RatingsCalculator)}: Player {before} -> {playerRating} (hitCap={hitCap}, correct={isCorrect}).");
        }

        return new RatingResult(playerRating, competitorSnapshot, playerState, hitCap);
    }

    private static int ClampRating(int value)
    {
        return Mathf.Clamp(value, MinRating, MaxRating);
    }

    private static RatingState ResolveState(int rating)
    {
        if (rating < 2500)
        {
            return RatingState.MuyMal;
        }

        if (rating < 5000)
        {
            return RatingState.Mal;
        }

        if (rating < 7500)
        {
            return RatingState.Promedio;
        }

        if (rating < 10000)
        {
            return RatingState.Bien;
        }

        return RatingState.MuyBien;
    }

    private void EnsureCompetitorArray()
    {
        if (competitorRatings != null && competitorRatings.Length == 3)
        {
            return;
        }

        int[] newRatings = new int[3] { 5000, 5000, 5000 };
        if (competitorRatings != null)
        {
            for (int i = 0; i < competitorRatings.Length && i < newRatings.Length; i++)
            {
                newRatings[i] = competitorRatings[i];
            }
        }

        competitorRatings = newRatings;
    }

    private static int[] GenerateDistribution(int total, int minPer, int count)
    {
        if (count <= 0)
        {
            return new int[0];
        }

        minPer = Mathf.Max(0, minPer);
        int minTotal = minPer * count;
        if (minTotal > total)
        {
            minPer = total / count;
            minTotal = minPer * count;
        }

        int remaining = total - minTotal;
        int[] distribution = new int[count];
        for (int i = 0; i < count; i++)
        {
            distribution[i] = minPer;
        }

        for (int i = 0; i < count; i++)
        {
            int extra = (i == count - 1) ? remaining : Random.Range(0, remaining + 1);
            distribution[i] += extra;
            remaining -= extra;
        }

        Shuffle(distribution);
        return distribution;
    }

    private static void Shuffle(int[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = array[i];
            array[i] = array[j];
            array[j] = temp;
        }
    }
}
