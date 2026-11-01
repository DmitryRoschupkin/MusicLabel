namespace MusicLabel;

public sealed record PricedRelease(string Id, ValidRelease Release, ReleaseQuote Quote);

public sealed record PriorityRow(string Id, string Artist, string Title, int Score, decimal Cost);

public sealed record TierRow(MusicTier Tier, int Count, decimal Total);

public sealed record TierFeeRow(MusicTier Tier, decimal Total, int RateBps, long FeeCents);

public sealed record ReportBundle(
    IReadOnlyList<PriorityRow> Priority,
    IReadOnlyList<TierFeeRow> TierFees,
    IReadOnlyList<Error> Rejected);