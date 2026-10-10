namespace Pos.Infrastructure.Services;

public sealed class StoreOptions
{
    public const string SectionName = "Store";

    /// <summary>IANA or Windows time-zone id used for day/week/month boundaries and receipts.</summary>
    public string TimeZoneId { get; init; } = "Asia/Karachi";
}
