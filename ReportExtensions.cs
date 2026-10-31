namespace MusicLabel;

public static class ReportExtensions
{
    public static IOrderedEnumerable<PriorityRow> OrderByPriority(this IEnumerable<PriorityRow> rows) =>
        rows.OrderByDescending(r => r.Score)
            .ThenBy(r => r.Title, StringComparer.Ordinal)
            .ThenBy(r => r.Id, StringComparer.Ordinal);
}