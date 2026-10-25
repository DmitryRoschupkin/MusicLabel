namespace MusicLabel;

public sealed record CatalogEntry
{
    public required string Id { get; init; }
    public required string ArtistName { get; init; }
    public required string ReleaseTitle { get; init; }
    public required string TracksCount { get; init; }
    public required string HasExplicitContent { get; init; }
    public required string TargetPlatformsCount { get; init; }
    public required string Tier { get; init; }

    public RawReleaseRecord ToRaw() =>
        new(ArtistName, ReleaseTitle, TracksCount, HasExplicitContent, TargetPlatformsCount, Tier);
}

public sealed class ReleaseCatalog
{
    private readonly IReadOnlyDictionary<string, RawReleaseRecord> _byId;
    private ReleaseCatalog(IReadOnlyDictionary<string, RawReleaseRecord> byId) => _byId = byId;
    
    public int Count => _byId.Count;

    public static Result<ReleaseCatalog> From(IEnumerable<CatalogEntry> entries)
    {
        var byId = new Dictionary<string, RawReleaseRecord>(StringComparer.OrdinalIgnoreCase);
        int index = 0;
        foreach (var entry in entries)
        {
            index++;
            if (entry is null || string.IsNullOrWhiteSpace(entry.Id))
                return Result<ReleaseCatalog>.Fail(Errors.CatalogIdMissing(index));
            if (!byId.TryAdd(entry.Id.Trim(), entry.ToRaw()))
                return Result<ReleaseCatalog>.Fail(Errors.DuplicateCatalogId(entry.Id.Trim()));
        }
        return Result<ReleaseCatalog>.Ok(new ReleaseCatalog(byId));
    }
    
    public Maybe<RawReleaseRecord> Find(string? id) =>
        id is not null && _byId.TryGetValue(id.Trim(), out var raw)
        ? Maybe.Some(raw)
        : Maybe.None<RawReleaseRecord>();
}