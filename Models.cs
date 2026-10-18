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

public record ValidRelease(
    string ArtistName,
    string ReleaseTitle,
    int TracksCount,
    bool HasExplicitContent,
    int TargetPlatformsCount,
    string Tier
);

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