namespace MusicLabel;

public class PipelineStages
{
    public static ValidRelease NormalizeRelease(ValidRelease validRelease)
    {
        return validRelease with
        {
            ReleaseTitle = validRelease.ReleaseTitle.Trim(),
            ArtistName = validRelease.ArtistName.Trim()
        };
    }

    public static (ValidRelease Release, decimal BaseCost) CalculateBaseCost(ValidRelease validRelease)
    {
        decimal costPerTrack = 20m;
        decimal costPerPlatform = 5m;

        decimal baseCost = (validRelease.TracksCount * costPerTrack) +
                           (validRelease.TargetPlatformsCount * costPerPlatform);

        if (validRelease.HasExplicitContent) baseCost += 15m;
        
        return (validRelease, baseCost);
    }

    public static ReleaseQuote FormatQuote((ValidRelease Release, decimal BaseCost) input,
        Func<ValidRelease, decimal, decimal> pricingRule)
    {
        var (release, baseCost) =  input;
        decimal finalCost = pricingRule(release, baseCost);
        
        string summary = $"Release '{release.ReleaseTitle}' by {release.ArtistName} " +
                         $"({release.TracksCount} tracks, {release.TargetPlatformsCount} platforms, Tier: {release.Tier}) " +
                         $"evaluated to ${finalCost}.";

        return new ReleaseQuote(
            release.ArtistName,
            release.ReleaseTitle,
            finalCost,
            release.Tier,
            summary
        );
    }

    public static ReleaseQuote ProcessRelease(ValidRelease rawRelease, Func<ValidRelease, decimal, decimal> pricingRule)
    {
        var normalized = NormalizeRelease(rawRelease);
        var costData = CalculateBaseCost(normalized);
        var quote = FormatQuote(costData, pricingRule);
        
        return quote;
    }
}