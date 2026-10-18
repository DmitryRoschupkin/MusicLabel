namespace MusicLabel;

public static class PricingRules
{
    public static decimal IndieRule(ValidRelease release, decimal baseCost)
    {
        decimal discount = release.TargetPlatformsCount > 10 ? 0.90m : 1.0m;
        return Math.Round(baseCost * discount, 2);
    }

    public static decimal MajorRule(ValidRelease release, decimal baseCost)
    {
        decimal priorityFee = 100.0m;
        return Math.Round(baseCost + priorityFee + (release.TracksCount * 15.0m), 2);
    }

    public static Func<ValidRelease, decimal, decimal> ForTier(MusicTier tier) => tier switch
    {
        MusicTier.Indie => IndieRule,
        MusicTier.Major => MajorRule,
        _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, "Unknown tier")
    };
}