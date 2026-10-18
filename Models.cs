namespace MusicLabel;

public record RawReleaseRecord
(
    string ArtistName,
    string ReleaseTitle,
    string TracksCountStr,
    string HasExplicitContentStr,
    string TargetPlatformsCountStr,
    string TierStr
);

public sealed record ValidRelease
{
    public string ArtistName { get ; private init; }
    public string ReleaseTitle { get ; private init; }
    public int TracksCount  {get ; private init; }
    public bool HasExplicitContent { get; private init; }
    public int TargetPlatformsCount { get; private init; }
    public MusicTier Tier { get; private init; }

    private ValidRelease(string artistName, string releaseTitle, int tracksCount, bool hasExplicitContent,
        int targetPlatformsCount, MusicTier tier)
    {
        ArtistName = artistName;
        ReleaseTitle = releaseTitle;
        TracksCount = tracksCount;
        HasExplicitContent = hasExplicitContent;
        TargetPlatformsCount = targetPlatformsCount;
        Tier = tier;
    }

    public static ReleaseResult Create(string artistName, string releaseTitle,
        int tracksCount, bool hasExplicitContent, int targetPlatformsCount, MusicTier tier)
    {
        if (string.IsNullOrWhiteSpace(artistName))
            return new ReleaseResult.Invalid("Validation Error: ArtistName is required.");
        if (string.IsNullOrWhiteSpace(releaseTitle))
            return new ReleaseResult.Invalid("Validation Error: ReleaseTitle is required.");
        if (tracksCount <= 0)
            return new ReleaseResult.Invalid($"Validation Error: Invalid tracks count ({tracksCount})," +
                                             $" must be strictly > 0.");
        if (targetPlatformsCount < 1 || targetPlatformsCount > 25)
            return new ReleaseResult.Invalid($"Validation Error: Target platforms count ({targetPlatformsCount})" +
                                             $" must be between 1 and 25.");
        if (!Enum.IsDefined(tier))
            return new ReleaseResult.Invalid($"Validation Error: Invalid tier ({tier}).");
        return new ReleaseResult.Valid(new ValidRelease(
            artistName.Trim(),
            releaseTitle.Trim(),
            tracksCount,
            hasExplicitContent,
            targetPlatformsCount,
            tier
            ));
    }
};

public record ReleaseQuote(
    string ArtistName,
    string ReleaseTitle,
    decimal TotalCost,
    string AppliedRule,
    string Summary
);

public enum MusicTier
{
    Indie,
    Major
}

public abstract record ReleaseResult
{
    private ReleaseResult() { }

    public record Valid(ValidRelease Release) : ReleaseResult;

    public record Invalid(string Reason) : ReleaseResult;
}