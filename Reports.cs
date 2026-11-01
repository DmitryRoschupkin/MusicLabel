namespace MusicLabel;

public static class PriorityReport
{
    public static IReadOnlyList<PriorityRow> Build(IEnumerable<PricedRelease> items,
        Func<ValidRelease, int> score, int minScore) =>
        items
            .Select(p => new PriorityRow(p.Id, p.Release.ArtistName, p.Release.ReleaseTitle,
                score(p.Release), p.Quote.TotalCost))
            .Where(r => r.Score >= minScore)
            .OrderByPriority()
            .ToList();
}


public static class TierReport
{
    public static IReadOnlyList<TierRow> Build(IEnumerable<PricedRelease> items) =>
        items
            .GroupBy(p => p.Release.Tier)
            .Select(g => new TierRow(g.Key, g.Count(), g.Sum(p => p.Quote.TotalCost)))
            .OrderByDescending(r => r.Total)
            .ThenBy(r => r.Tier)
            .ToList();
}