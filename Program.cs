namespace MusicLabel;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Assignment 5: Music Label Distribution Pipeline");
        var rawDataset = new List<RawReleaseRecord>
        {
            new("Friendly Thug 52 NGG", "COW", "1", "true", "5", "Indie"),
            new("Автоспорт", "Береги себя", "8", "false", "15", "Major"),
            new("Mnogoznaal", "Гостиница Космос", "0", "false", "5", "Indie"),
            new("Test Artist", "Boundary test", "1", "false", "1", "Indie"),
            new ("Test Artist", "Boundary OK", "1", "false", "25", "Indie"),
            new("dmitry_rspkn", "Kolutonskiy Tape", "3", "false", "20", "Major")
        };
        
        Console.WriteLine("\nRUNNING WORKFLOW PIPELINE (pricing rule is chosen by release tier)");
        RunWorkflowPipeline(rawDataset);
        
        Console.WriteLine("\nDEMO: MISSING PREREQUISITE (ProcessStep without ValidateStep)");
        var skipped = WorkflowTransitions.ProcessStep(new WorkflowState.Pending(rawDataset[0]));
        Console.WriteLine(Render(skipped));
    }

    private static void RunWorkflowPipeline(IEnumerable<RawReleaseRecord> rawRecords)
    {
        foreach (var rawRecord in rawRecords)
        {
            Console.WriteLine($"Processing input for artist: {rawRecord.ArtistName} ({rawRecord.ReleaseTitle})");
            
            WorkflowState state = new WorkflowState.Pending(rawRecord);

            state = WorkflowTransitions.ValidateStep(state);
            
            state = WorkflowTransitions.ProcessStep(state);
            
            Console.WriteLine(Render(state));

            if (state is WorkflowState.Processed)
            {
                var repeatedState = WorkflowTransitions.ProcessStep(state);
                Console.WriteLine(repeatedState == state
                    ? "[SAFE REPEAT] Repeated operation returned the existing outcome, no recalculation."
                    : "[ERROR] Repeated operation changed the state");
            }
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