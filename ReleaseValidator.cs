using System.Globalization;

namespace MusicLabel;

public static class ReleaseValidator
{
    public static Result<ValidRelease> ParseAndValidate(RawReleaseRecord raw)
    {
        if (!int.TryParse(raw.TracksCountStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int tracksCount))
            return Result<ValidRelease>.Fail(Errors.MalformedInput(
                $"Tracks count '{raw.TracksCountStr}' is not a whole number."));

        if (!bool.TryParse(raw.HasExplicitContentStr, out bool hasExplicit))
            return Result<ValidRelease>.Fail(Errors.MalformedInput(
                $"Explicit flag '{raw.HasExplicitContentStr}' must be true or false."));

        if (!int.TryParse(raw.TargetPlatformsCountStr, NumberStyles.Integer, CultureInfo.InvariantCulture,
                out int targetPlatformsCount))
            return Result<ValidRelease>.Fail(Errors.MalformedInput(
                $"Platforms count '{raw.TargetPlatformsCountStr}' is not a whole number."));

        string cleanTier = raw.TierStr?.Trim() ?? string.Empty;
        if (cleanTier.Length == 0
            || !cleanTier.All(char.IsLetter)
            || !Enum.TryParse<MusicTier>(cleanTier, true, out MusicTier tier))
            return Result<ValidRelease>.Fail(Errors.InvalidTier(cleanTier));

        return ValidRelease.Create(raw.ArtistName, raw.ReleaseTitle, tracksCount,
            hasExplicit, targetPlatformsCount, tier);
    }
}