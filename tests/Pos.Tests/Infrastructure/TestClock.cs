using Pos.Application.Common.Abstractions.Services;

namespace Pos.Tests.Infrastructure;

public sealed class TestClock : IDateTimeProvider
{
    public DateTime UtcNow { get; set; } = new(2026, 10, 10, 7, 0, 0, DateTimeKind.Utc);
}
