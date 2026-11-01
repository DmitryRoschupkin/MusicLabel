namespace MusicLabel;

public static class ReportData
{
    public static IEnumerable<Result<PricedRelease>> AnalyzeLazily(
        IEnumerable<(string Id, RawReleaseRecord Raw)> entries,
        Func<string, RawReleaseRecord, Result<PricedRelease>> analyze)
    {
        foreach (var (id, raw) in entries)
            yield return analyze(id, raw);
    }
    public static Result<PricedRelease> Analyze(string id, RawReleaseRecord raw) =>
        ReleaseValidator.ParseAndValidate(raw)
            .Bind(release => QuoteWorkflow.Price(new WorkflowState.Validated(release))
                .Map(LoyaltyProgram.Apply)
                .Map(qoute => new PricedRelease(id, release, qoute)))
            .MapError(e => new Error(e.Code, $"[{id}] {e.Message}", e.Kind));

    public static (IReadOnlyList<PricedRelease> Priced, IReadOnlyList<Error> Rejected) Load(
        ReleaseCatalog catalog,
        Func<string, RawReleaseRecord, Result<PricedRelease>>? analyze = null)
    {
        analyze ??= Analyze;
        
        var snapshot = AnalyzeLazily(catalog.Entries, analyze).ToList();
        return snapshot.Partition();
    }
}