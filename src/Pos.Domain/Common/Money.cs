namespace Pos.Domain.Common;

public static class Money
{
    /// <summary>Rounds a monetary amount to 2 decimals using commercial (away from zero) rounding.</summary>
    public static decimal Round(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);
}
