namespace MusicLabel;

public static class Errors
{
    public static Error ArtistRequired() =>
        new("ARTIST_REQUIRED", "Validation Error: ArtistName is required.");

    public static Error TitleRequired() =>
        new("TITLE_REQUIRED", "Validation Error: ReleaseTitle is required.");

    public static Error InvalidTracksCount(int count) =>
        new("INVALID_TRACKS_COUNT",
            $"Validation Error: Invalid tracks count ({count}), must be strictly > 0.");

    public static Error InvalidPlatformsCount(int count) =>
        new("INVALID_PLATFORMS_COUNT",
            $"Validation Error: Target platforms count ({count}) must be between 1 and 25.");

    public static Error InvalidTier(object tier) =>
        new("INVALID_TIER", $"Validation Error: Invalid tier ({tier}).");

    public static Error MalformedInput(string detail) =>
        new("MALFORMED_INPUT", $"Validation Error: {detail}");

    public static Error ReleaseNotFound(string id) =>
        new("RELEASE_NOT_FOUND", $"Release '{id}' was not found in the catalog.");

    public static Error PrerequisiteNotMet() =>
        new("PREREQUISITE_NOT_MET",
            "Prerequisite not met: Release must be validated before processing.");

    public static Error InvalidTransition(string fromState) =>
        new("INVALID_TRANSITION", $"Invalid state transition from {fromState}.");
}