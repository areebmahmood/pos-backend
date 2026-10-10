using Pos.Application.Common.Models;

namespace Pos.Application.Common.Abstractions.Services;

/// <summary>Converts between UTC and the store's local time zone (used for day/week/month boundaries).</summary>
public interface IStoreClock
{
    string TimeZoneId { get; }

    DateOnly Today { get; }

    DateTime ToLocal(DateTime utc);

    DateTime ToUtc(DateTime local);

    /// <summary>UTC range covering the local calendar dates [from, to] inclusive.</summary>
    DateRange ToUtcRange(DateOnly from, DateOnly to);

    /// <summary>Offset of local time from UTC, in minutes, at the given UTC instant.</summary>
    int OffsetMinutes(DateTime utc);
}
