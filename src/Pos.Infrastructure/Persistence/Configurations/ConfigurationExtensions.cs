using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Pos.Infrastructure.Persistence.Configurations;

internal static class ConfigurationExtensions
{
    public const int QuantityPrecision = 18;
    public const int QuantityScale = 3;

    public static PropertyBuilder<decimal> IsQuantity(this PropertyBuilder<decimal> property) =>
        property.HasPrecision(QuantityPrecision, QuantityScale);
}
