namespace MusicLabel;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Assignment 7: Exception Handling in FP");
        Console.WriteLine("\nEarlier pipeline from previous assignments: ");
        RunLegacyPipeline();
        
        Console.WriteLine("\n=== File failures (boundary) ===");
        Report("A. Missing file", CatalogLoader.Load("sample-data/does-not-exist.json").Map(Describe));
        Report("B. Malformed JSON", CatalogLoader.Load("sample-data/malformed.json").Map(Describe));

        var loaded = CatalogLoader.Load("sample-data/catalog.json");
        switch (loaded)
        {
            case Result<ReleaseCatalog>.Success(var catalog):
                Console.WriteLine($"\nCatalog loaded: {catalog.Count} release(s).");
                RunCatalogDemos(catalog);
                break;
            case Result<ReleaseCatalog>.Failure(var error):
                Console.WriteLine($"\n[{error.Kind} FAILURE] {error}");
                break;
        }
    }

    private static void RunCatalogDemos(ReleaseCatalog catalog)
    {
        Console.WriteLine("\n=== Lookup: present and missing ===");
        Console.WriteLine(DescribeLookup(catalog.Find("REL-001")));
        Console.WriteLine(DescribeLookup(catalog.Find("REL-999")));
        
        Console.WriteLine("\n=== Result chain ===");
        Report("1. Normal path (REL-002)", QuoteWorkflow.Quote(catalog, "REL-002"));
        Report("2. Domain failure: invalid quantity (REL-003)", QuoteWorkflow.Quote(catalog, "REL-003"));
        Report("3. Domain failure: missing required item (REL-999)", QuoteWorkflow.Quote(catalog, "REL-999"));
        Report("4. Domain failure: state forbids pricing",
            QuoteWorkflow.Price(new WorkflowState.Pending(new RawReleaseRecord("A", "T", "1", "false", "5", "Indie")))
                .Map(q => q.Summary));
        Report("5. Optional value: Some(0) loyalty (REL-004)", QuoteWorkflow.Quote(catalog, "REL-004"));
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
        WorkflowState.Failed(var reason) =>
            $"[REJECTED] {reason}",
        WorkflowState.Pending => "[INCOMPLETE] Release is still pending validation",
        WorkflowState.Validated => "[INCOMPLETE] Release is validated but not yet priced",
        _ => "[REJECTED] Unknown workflow state."
    };
}