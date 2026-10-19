namespace Pos.Domain.Enums;

/// <summary>Which online channel an <see cref="PaymentMethod.Online"/> bill was paid through.</summary>
public enum OnlinePaymentType
{
    JazzCash = 1,
    EasyPaisa = 2,
    BankTransfer = 3
}
