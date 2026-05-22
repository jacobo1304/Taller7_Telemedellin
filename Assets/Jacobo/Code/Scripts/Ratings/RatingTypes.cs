using System;

public enum RatingState
{
    MuyMal,
    Mal,
    Promedio,
    Bien,
    MuyBien
}

[Serializable]
public struct RatingResult
{
    public int PlayerRating;
    public int[] CompetitorRatings;
    public RatingState PlayerState;
    public bool HitCap;

    public RatingResult(int playerRating, int[] competitorRatings, RatingState playerState, bool hitCap)
    {
        PlayerRating = playerRating;
        CompetitorRatings = competitorRatings;
        PlayerState = playerState;
        HitCap = hitCap;
    }
}
