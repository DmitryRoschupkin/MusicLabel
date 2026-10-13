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
            new("dmitry_rspkn", "Kolutonskiy Tape", "3", "false", "20", "Major")
        };
        
        Console.WriteLine("RUNNING WITH INDIE RULE");
        RunPipelineWithRule(rawDataset, PricingRules.IndieRule, "Indie rule");
        
        Console.WriteLine("RUNNING WITH MAJOR RULE");
        RunPipelineWithRule(rawDataset, PricingRules.MajorRule, "Major rule");
    }

    private static void RunPipelineWithRule(
        IEnumerable<RawReleaseRecord> rawRecords,
        Func<ValidRelease, decimal, decimal> rule,
        string ruleName
    )
    {
        foreach (var rawRecord in rawRecords)
        {
            Console.WriteLine($"Processing input for artist: {rawRecord.ArtistName} ({rawRecord.ReleaseTitle})");
            
            var (validRelease, error) = ReleaseValidator.ParseAndValidate(rawRecord);

            if (error != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  [REJECTED] {error}");
                Console.ResetColor();
                continue;
            }
            
            var quote = PipelineStages.ProcessRelease(validRelease!, rule);
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  [SUCCESS] Rule: {quote.AppliedRule} | Final Cost: ${quote.TotalCost}");
            Console.WriteLine(  $"            Summary: {quote.Summary}");
            Console.ResetColor();
        }
    }
}