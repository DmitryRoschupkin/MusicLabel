namespace MusicLabel;

public static class LoyaltyProgram
{
    private static readonly IReadOnlyDictionary<string, decimal> Discounts =
        new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["Mnogoznaal"] = 0.15m,
            ["Test Artist"] = 0m
        };
    
    public static Maybe<decimal> DiscountFor(string artist) =>
        Discounts.TryGetValue(artist, out var d) ? Maybe.Some(d) : Maybe.None<decimal>();

    public static ReleaseQuote Apply(ReleaseQuote quote) => DiscountFor(quote.ArtistName) switch
    {
        Maybe<decimal>.Some(var discount) => quote with
        {
            TotalCost = Math.Round(quote.TotalCost * (1m - discount), 2),
            AppliedRule = FormattableString.Invariant($"{quote.AppliedRule}+Loyalty({discount:P0})"),
            Summary = FormattableString.Invariant($"{quote.Summary} Loyalty discount {discount:P0} applied.")
        },
        _ => quote
    };
}