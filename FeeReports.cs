namespace MusicLabel;

public static class FeeSchedule
{
    public static int RateBps(MusicTier tier) => tier switch
    {
        MusicTier.Indie => 500,
        MusicTier.Major => 1200,
        _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, "Unknown tier.")
    };
}

public static class FeeReport
{
    public static Result<IReadOnlyList<TierFeeRow>> Build(
        IEnumerable<TierRow> tiers, Func<long, int, Result<long>> fee) =>
        tiers.Select(row => FeeFor(row, fee)).Sequence();

    private static Result<TierFeeRow> FeeFor(TierRow row, Func<long, int, Result<long>> fee)
    {
        long cents = (long)Math.Round(row.Total * 100m, 0, MidpointRounding.AwayFromZero);
        int rate = FeeSchedule.RateBps(row.Tier);
        return fee(cents, rate).Map(feeCents => new TierFeeRow(row.Tier, row.Total, rate, feeCents));
    }
}

public static class ReportWorkflow
{
    public static Result<ReportBundle> Build(ReleaseCatalog catalog, Func<ValidRelease, int> score,
        int minScore, Func<long, int, Result<long>> fee)
    {
        var (priced, rejected) = ReportData.Load(catalog);
        var priority = PriorityReport.Build(priced, score, minScore);

        return FeeReport.Build(TierReport.Build(priced), fee)
            .Map(tierFees => new ReportBundle(priority, tierFees, rejected));
    }

    public static Result<ReportBundle> BuildAndExport(ReleaseCatalog catalog, Func<ValidRelease, int> score,
        int minScore, Func<long, int, Result<long>> fee, string outputPath) =>
        Build(catalog, score, minScore, fee)
            .Bind(bundle => ReportExporter.WriteFile(outputPath, ReportCsv.TierFeeLines(bundle.TierFees))
                .Map(_ => bundle));
}