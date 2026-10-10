using Pos.Application.Common.Abstractions.Services;

namespace Pos.Application.Common.Models;

public static class DateFilter
{
    private static readonly DateOnly Epoch = new(2000, 1, 1);

    /// <summary>
    /// Converts optional store-local dates (inclusive) to a UTC range; returns <c>null</c> when neither bound is given.
    /// </summary>
    public static DateRange? ToUtcRange(this IStoreClock clock, DateOnly? from, DateOnly? to)
    {
        if (from is null && to is null)
            return null;

        return clock.ToUtcRange(from ?? Epoch, to ?? clock.Today);
    }
}
