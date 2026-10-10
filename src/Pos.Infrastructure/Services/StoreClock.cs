using Microsoft.Extensions.Options;
using Pos.Application.Common.Abstractions.Services;
using Pos.Application.Common.Models;

namespace Pos.Infrastructure.Services;

internal sealed class StoreClock(IOptions<StoreOptions> options, IDateTimeProvider clock) : IStoreClock
{
    private readonly TimeZoneInfo _zone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZoneId);

    public string TimeZoneId => options.Value.TimeZoneId;

    public DateOnly Today => DateOnly.FromDateTime(ToLocal(clock.UtcNow));

    public DateTime ToLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), _zone);

    public DateTime ToUtc(DateTime local) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), _zone);

    public DateRange ToUtcRange(DateOnly from, DateOnly to) =>
        new(ToUtc(from.ToDateTime(TimeOnly.MinValue)), ToUtc(to.AddDays(1).ToDateTime(TimeOnly.MinValue)));

    public int OffsetMinutes(DateTime utc) =>
        (int)_zone.GetUtcOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)).TotalMinutes;
}
