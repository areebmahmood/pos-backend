using System.Globalization;

namespace Pos.Application.Features.Reports;

/// <summary>Pure calendar logic for report periods and their chart buckets (store-local time).</summary>
public static class ReportPeriodCalculator
{
    public static (DateOnly From, DateOnly To) GetRange(ReportPeriod period, DateOnly date) => period switch
    {
        ReportPeriod.Daily => (date, date),
        ReportPeriod.Weekly => WeekOf(date),
        ReportPeriod.Monthly => (new DateOnly(date.Year, date.Month, 1),
            new DateOnly(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month))),
        ReportPeriod.Yearly => (new DateOnly(date.Year, 1, 1), new DateOnly(date.Year, 12, 31)),
        _ => throw new ArgumentOutOfRangeException(nameof(period), period, null)
    };

    /// <summary>Start of the bucket a local timestamp falls into.</summary>
    public static DateTime BucketStart(ReportPeriod period, DateTime local) => period switch
    {
        ReportPeriod.Daily => new DateTime(local.Year, local.Month, local.Day, local.Hour, 0, 0),
        ReportPeriod.Yearly => new DateTime(local.Year, local.Month, 1),
        _ => local.Date
    };

    /// <summary>All bucket starts covering [from, to], so empty buckets still appear in charts.</summary>
    public static IEnumerable<DateTime> EnumerateBuckets(ReportPeriod period, DateOnly from, DateOnly to)
    {
        var cursor = from.ToDateTime(TimeOnly.MinValue);
        var end = to.ToDateTime(TimeOnly.MaxValue);
        while (cursor <= end)
        {
            yield return cursor;
            cursor = period switch
            {
                ReportPeriod.Daily => cursor.AddHours(1),
                ReportPeriod.Yearly => cursor.AddMonths(1),
                _ => cursor.AddDays(1)
            };
        }
    }

    public static string Label(ReportPeriod period, DateTime bucketStart) => period switch
    {
        ReportPeriod.Daily => bucketStart.ToString("HH:00", CultureInfo.InvariantCulture),
        ReportPeriod.Weekly => bucketStart.ToString("ddd dd MMM", CultureInfo.InvariantCulture),
        ReportPeriod.Monthly => bucketStart.ToString("dd MMM", CultureInfo.InvariantCulture),
        ReportPeriod.Yearly => bucketStart.ToString("MMM yyyy", CultureInfo.InvariantCulture),
        _ => bucketStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
    };

    private static (DateOnly, DateOnly) WeekOf(DateOnly date)
    {
        // ISO weeks start on Monday.
        var offset = ((int)date.DayOfWeek + 6) % 7;
        var monday = date.AddDays(-offset);
        return (monday, monday.AddDays(6));
    }
}
