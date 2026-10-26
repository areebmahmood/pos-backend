using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pos.Api.Infrastructure;

/// <summary>
/// Every timestamp is stored in UTC, but SQL Server hands them back with an unspecified kind, which
/// would be serialized without an offset and read by browsers as their own local time. This writes
/// them with a trailing <c>Z</c> so clients convert them to the store's time zone correctly.
/// </summary>
internal sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetDateTime();
        return value.Kind switch
        {
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value
        };
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utc = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        writer.WriteStringValue(utc.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", CultureInfo.InvariantCulture));
    }
}
