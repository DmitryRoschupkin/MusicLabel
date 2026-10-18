namespace MusicLabel;

public static class PipelineStages
{
    public static decimal CalculateBaseCost(ValidRelease validRelease)
    {
        decimal costPerTrack = 20m;
        decimal costPerPlatform = 5m;

        decimal baseCost = (validRelease.TracksCount * costPerTrack) +
                           (validRelease.TargetPlatformsCount * costPerPlatform);

        if (validRelease.HasExplicitContent) baseCost += 15m;
        
        return baseCost;
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
            release.Tier.ToString(),
            summary
        );
    }

    public static ReleaseQuote ExecutePricing(ValidRelease release, Func<ValidRelease, decimal, decimal> pricingRule)
        => FormatQuote((release, CalculateBaseCost(release)), pricingRule);
}