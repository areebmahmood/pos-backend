using Pos.Application.Common.Models;
using Pos.Domain.Enums;

namespace Pos.Application.Features.Registers;

public sealed record RegisterSessionDto(
    int Id,
    int UserId,
    string UserName,
    RegisterStatus Status,
    DateTime OpenedAt,
    DateTime? ClosedAt,
    decimal OpeningCash,
    decimal CashSales,
    decimal CardSales,
    decimal OnlineSales,
    decimal CashRefunds,
    decimal Expenses,
    decimal ExpectedCash,
    decimal? ClosingCash,
    decimal? Difference,
    int SaleCount,
    string? OpeningNotes,
    string? ClosingNotes);

public sealed record OpenRegisterRequest(decimal OpeningCash, string? Notes);

public sealed record CloseRegisterRequest(decimal ClosingCash, string? Notes);

public sealed record RegisterQuery : PageRequest
{
    public RegisterStatus? Status { get; init; }
    public int? UserId { get; init; }
}

/// <summary>Register columns read from the database before cash totals are attached.</summary>
public sealed record RegisterRow(
    int Id,
    int UserId,
    string UserName,
    RegisterStatus Status,
    DateTime OpenedAt,
    DateTime? ClosedAt,
    decimal OpeningCash,
    decimal? ExpectedCash,
    decimal? ClosingCash,
    string? OpeningNotes,
    string? ClosingNotes);
