namespace MusicLabel;

public enum ErrorKind { Domain, Operational }

public sealed record Error(string Code, string Message, ErrorKind Kind = ErrorKind.Domain)
{
    public override string ToString() => $"[{Code}] {Message}";
}

public static class Errors
{
    public static Error CatalogIdMissing(int index) =>
        new("CATALOG_ID_MISSING", $"Catalog entry #{index} has no id.", ErrorKind.Operational);

    public static Error DuplicateCatalogId(string id) =>
        new("DUPLICATE_CATALOG_ID", $"Catalog contains more than one entry with id '{id}'.", ErrorKind.Operational);

    public static Error FileNotFound(string path) =>
        new("FILE_NOT_FOUND", $"Catalog file '{path}' was not found.", ErrorKind.Operational);

    public static Error FileUnreadable(string path, string detail) =>
        new("FILE_UNREADABLE", $"Catalog file '{path}' could not be read: {detail}", ErrorKind.Operational);

    public static Error MalformedFile(string path, string detail) =>
        new("MALFORMED_FILE", $"Catalog file '{path}' contains malformed JSON: {detail}", ErrorKind.Operational);
    
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