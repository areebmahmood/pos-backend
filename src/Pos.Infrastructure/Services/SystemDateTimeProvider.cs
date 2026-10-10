using Pos.Application.Common.Abstractions.Services;

namespace Pos.Infrastructure.Services;

internal sealed class SystemDateTimeProvider(TimeProvider timeProvider) : IDateTimeProvider
{
    public DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;
}
