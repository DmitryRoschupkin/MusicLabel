using System.Globalization;
namespace MusicLabel;

public static class ReportCsv
{
    public static IEnumerable<string> TierFeeLines(IEnumerable<TierFeeRow> rows)
    {
        yield return "tier,total,fee_rate_bps,service_fee_cents";
        foreach (var r in rows)
            yield return string.Join(',',
                r.Tier,
                r.Total.ToString("F2", CultureInfo.InvariantCulture),
                r.RateBps.ToString(CultureInfo.InvariantCulture),
                r.FeeCents.ToString(CultureInfo.InvariantCulture));
    }
}