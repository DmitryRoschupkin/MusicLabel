namespace MusicLabel;

public sealed record PricesRelease(string Id, ValidRelease Release, ReleaseQuote Quote);

public sealed record PriorityRow(string Id, string Artist, string Title, int Score, decimal Cost);

public sealed record TierRow(MusicTier Tier, int Count, decimal Total);