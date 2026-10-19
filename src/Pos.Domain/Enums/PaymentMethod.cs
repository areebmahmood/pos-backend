namespace Pos.Domain.Enums;

public enum PaymentMethod
{
    Cash = 1,
    Card = 2,

    /// <summary>Paid through a mobile wallet or bank transfer; see <see cref="OnlinePaymentType"/>.</summary>
    Online = 3
}
