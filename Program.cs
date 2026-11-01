namespace MusicLabel;

class Program
{
    private const string CatalogPath = "sample-data/catalog.json";

    static void Main()
    {
        Console.WriteLine("MusicLabel: release distribution quotes (Assignments 5-8)");

        Section("Earlier pipeline (Assignments 5-6)");
        RunLegacyPipeline();

        Section("File failures at the boundary (Assignment 7)");
        Report("A. Missing file", CatalogLoader.Load("sample-data/does-not-exist.json").Map(Describe));
        Report("B. Malformed JSON", CatalogLoader.Load("sample-data/malformed.json").Map(Describe));

        // Каталог загружается один раз, дальше все демо работают с готовым снимком.
        switch (CatalogLoader.Load(CatalogPath))
        {
            case Result<ReleaseCatalog>.Success(var catalog):
                Console.WriteLine($"\nCatalog loaded: {catalog.Count} release(s).");
                RunCatalogDemos(catalog);
                RunReports(catalog);
                RunIntegratedWorkflow(catalog);
                break;
            case Result<ReleaseCatalog>.Failure(var error):
                Console.WriteLine($"\n[{error.Kind.ToString().ToUpperInvariant()} FAILURE] {error}");
                break;
        }
    }
    
    private static void RunCatalogDemos(ReleaseCatalog catalog)
    {
        Section("Lookup: present and missing (Assignment 7)");
        Console.WriteLine(DescribeLookup(catalog.Find("REL-001")));
        Console.WriteLine(DescribeLookup(catalog.Find("REL-999")));

        Section("Result chain (Assignment 7)");
        Report("1. Normal path (REL-002)", QuoteWorkflow.Quote(catalog, "REL-002"));
        Report("2. Domain failure: invalid quantity (REL-003)", QuoteWorkflow.Quote(catalog, "REL-003"));
        Report("3. Domain failure: missing required item (REL-999)", QuoteWorkflow.Quote(catalog, "REL-999"));
        Report("4. Domain failure: state forbids pricing",
            QuoteWorkflow.Price(new WorkflowState.Pending(new RawReleaseRecord("A", "T", "1", "false", "5", "Indie")))
                .Map(q => q.Summary));
        Report("5. Optional value: Some(0) loyalty (REL-004)", QuoteWorkflow.Quote(catalog, "REL-004"));
    }
    
    private static void RunReports(ReleaseCatalog catalog)
    {
        Section("LINQ reports (Assignment 8)");
        var (priced, rejected) = ReportData.Load(catalog);

        foreach (var error in rejected)
            Console.WriteLine($"[REJECTED RECORD] {error}");

        var reach = Scoring.CreateScore(Scoring.ReachConfig);
        var album = Scoring.CreateScore(Scoring.AlbumConfig);

        PrintPriority("reach score", PriorityReport.Build(priced, reach, minScore: 20));
        PrintPriority("album score", PriorityReport.Build(priced, album, minScore: 20));
        PrintTiers(TierReport.Build(priced));
    }


    private static void RunIntegratedWorkflow(ReleaseCatalog catalog)
    {
        Section("Integrated workflow: reports + native C call + export (Assignment 8)");
        var score = Scoring.CreateScore(Scoring.ReachConfig);

        const string output = "report-output/tier-fees.csv";
        switch (ReportWorkflow.BuildAndExport(catalog, score, 20, NativeFee.Calculate, output))
        {
            case Result<ReportBundle>.Success(var bundle):
                foreach (var rejected in bundle.Rejected)
                    Console.WriteLine($"[REJECTED RECORD] {rejected}");
                PrintPriority("reach score", bundle.Priority);
                PrintTierFees(bundle.TierFees);
                Console.WriteLine($"[EXPORTED] {Path.GetFullPath(output)}");
                break;
            case Result<ReportBundle>.Failure(var error):
                Console.WriteLine($"[{error.Kind.ToString().ToUpperInvariant()} FAILURE] {error}");
                break;
        }

        const string failedOutput = "report-output/must-not-exist.csv";
        Func<long, int, Result<long>> brokenNative =
            (_, _) => Result<long>.Fail(Errors.NativeLibraryMissing("simulated for the demonstration"));

        var failed = ReportWorkflow.BuildAndExport(catalog, score, 20, brokenNative, failedOutput);
        if (failed is Result<ReportBundle>.Failure(var e))
            Console.WriteLine($"[{e.Kind.ToString().ToUpperInvariant()} FAILURE] {e}");
        Console.WriteLine(File.Exists(failedOutput)
            ? "[ERROR] a partial export was left behind"
            : "[OK] no export file was created after the failure");
    }


    private static void Section(string title) => Console.WriteLine($"\n=== {title} ===");

    private static void PrintPriority(string title, IReadOnlyList<PriorityRow> rows)
    {
        Console.WriteLine($"--- Priority report ({title})");
        if (rows.Count == 0)
        {
            Console.WriteLine("(no releases match)");
            return;
        }
        foreach (var r in rows)
            Console.WriteLine($"{r.Score,5}  {r.Id}  {r.Artist} - {r.Title}  (cost {r.Cost:F2})");
    }

    private static void PrintTiers(IReadOnlyList<TierRow> rows)
    {
        Console.WriteLine("--- Revenue by tier");
        if (rows.Count == 0)
        {
            Console.WriteLine("(no data)");
            return;
        }
        foreach (var r in rows)
            Console.WriteLine($"{r.Tier,-6} releases: {r.Count}  total: {r.Total:F2}");
    }

    private static void PrintTierFees(IReadOnlyList<TierFeeRow> rows)
    {
        Console.WriteLine("--- Service fee by tier (computed by the native C function)");
        foreach (var r in rows)
            Console.WriteLine($"{r.Tier,-6} total: {r.Total:F2}  rate: {r.RateBps} bps  fee: {r.FeeCents / 100m:F2}");
    }

    private static string DescribeLookup(Maybe<RawReleaseRecord> found) => found switch
    {
        Maybe<RawReleaseRecord>.Some(var raw) => $"[FOUND] {raw.ArtistName} - {raw.ReleaseTitle}",
        Maybe<RawReleaseRecord>.None => "[NOT FOUND] no such release in the catalog",
        _ => "[UNKNOWN]"
    };

    private static string Describe(ReleaseCatalog c) => $"Loaded {c.Count} release(s).";

    private static void Report(string label, Result<string> result)
    {
        Console.WriteLine($"---{label}");
        Console.WriteLine(result switch
        {
            Result<string>.Success(var text) => $"[SUCCESS] {text}",
            Result<string>.Failure(var e) => $"[{e.Kind.ToString().ToUpperInvariant()} FAILURE] {e}",
            _ => "[UNKNOWN RESULT]"
        });
    }
    
    private static void RunLegacyPipeline()
    {
        var rawDataset = new List<RawReleaseRecord>
        {
            new("Friendly Thug 52 NGG", "COW", "1", "true", "5", "Indie"),
            new("Автоспорт", "Береги себя", "8", "false", "15", "Major"),
            new("Mnogoznaal", "Гостиница Космос", "0", "false", "5", "Indie"),
        };
        foreach (var raw in rawDataset)
        {
            WorkflowState state = new WorkflowState.Pending(raw);
            state = WorkflowTransitions.ValidateStep(state);
            state = WorkflowTransitions.ProcessStep(state);
            Console.WriteLine(Render(state));
            if (state is WorkflowState.Processed)
                Console.WriteLine(WorkflowTransitions.ProcessStep(state) == state
                    ? "[SAFE REPEAT] Repeated operation returned the existing outcome."
                    : "[ERROR] Repeated operation changed the state");
        }
    }

    private static string Render(WorkflowState state) => state switch
    {
        WorkflowState.Processed(_, var quote) =>
            $"[SUCCESS] Tier: {quote.AppliedRule} | Final Cost: {quote.TotalCost} \nSummary: {quote.Summary}",
        WorkflowState.Failed(var error) => $"[REJECTED] {error}",
        WorkflowState.Pending => "[INCOMPLETE] Release is still pending validation",
        WorkflowState.Validated => "[INCOMPLETE] Release is validated but not yet priced",
        _ => "[REJECTED] Unknown workflow state."
    };
}