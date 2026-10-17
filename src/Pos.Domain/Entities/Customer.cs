using Pos.Domain.Common;
using Pos.Domain.Exceptions;

namespace Pos.Domain.Entities;

/// <summary>
/// A customer known by phone number. Created automatically when a bill has a phone number,
/// and used for WhatsApp thank-you and promotional messages.
/// </summary>
public class Customer : AuditableEntity
{
    public const int NameMaxLength = Sale.CustomerNameMaxLength;

    private readonly List<Sale> _sales = [];

    private Customer() { }

    /// <param name="phone">Already normalized with <see cref="PhoneNumber.Normalize"/>.</param>
    public Customer(string? name, string phone)
    {
        Name = Guard.Optional(name, "Customer name", NameMaxLength);
        Phone = RequirePhone(phone);
    }

    public string? Name { get; private set; }

    /// <summary>International digits without the plus, e.g. 923001234567.</summary>
    public string Phone { get; private set; } = default!;

    /// <summary>The customer asked not to receive promotional messages.</summary>
    public bool OptedOut { get; private set; }

    public IReadOnlyCollection<Sale> Sales => _sales;

    public void Update(string? name, string phone, bool optedOut)
    {
        Name = Guard.Optional(name, "Customer name", NameMaxLength);
        Phone = RequirePhone(phone);
        OptedOut = optedOut;
    }

    /// <summary>Fills in the name from a bill when the customer has none yet.</summary>
    public void RememberName(string? name)
    {
        if (Name is null)
            Name = Guard.Optional(name, "Customer name", NameMaxLength);
    }

    private static string RequirePhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone) || phone.Length > PhoneNumber.MaxLength || !phone.All(char.IsAsciiDigit))
            throw new DomainException("Enter a valid phone number.");
        return phone;
    }
}
