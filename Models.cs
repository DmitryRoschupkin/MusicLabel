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

    public static Result<ValidRelease> Create(string artistName, string releaseTitle,
        int tracksCount, bool hasExplicitContent, int targetPlatformsCount, MusicTier tier)
    {
        if (string.IsNullOrWhiteSpace(artistName))
            return Result<ValidRelease>.Fail(Errors.ArtistRequired());
        if (string.IsNullOrWhiteSpace(releaseTitle))
            return Result<ValidRelease>.Fail(Errors.TitleRequired());
        if (tracksCount <= 0)
            return Result<ValidRelease>.Fail(Errors.InvalidTracksCount(tracksCount));
        if (targetPlatformsCount < 1 || targetPlatformsCount > 25)
            return Result<ValidRelease>.Fail(Errors.InvalidPlatformsCount(targetPlatformsCount));
        if (!Enum.IsDefined(tier))
            return Result<ValidRelease>.Fail(Errors.InvalidTier(tier));
        return Result<ValidRelease>.Ok(new ValidRelease(
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