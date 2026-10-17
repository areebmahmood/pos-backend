using Pos.Application.Common.Models;

namespace Pos.Application.Features.Customers;

public sealed record CustomerDto(
    int Id,
    string? Name,
    string Phone,
    bool OptedOut,
    int PurchaseCount,
    decimal TotalSpent,
    DateTime? LastPurchaseAt,
    DateTime CreatedAt);

/// <summary>Name lookup used while billing.</summary>
public sealed record CustomerLookupDto(int Id, string? Name, string Phone);

public sealed record SaveCustomerRequest(string? Name, string Phone, bool OptedOut = false);

public sealed record CustomerQuery : PageRequest
{
    public string? Search { get; init; }

    /// <summary>Omit for everyone; <c>false</c> lists only customers who accept promotions.</summary>
    public bool? OptedOut { get; init; }
}

public sealed record CustomerFilter(PageRequest Page, string? Search, bool? OptedOut);
