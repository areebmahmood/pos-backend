using System.Linq.Expressions;
using Pos.Application.Common.Abstractions.Persistence;
using Pos.Domain.Common;
using Pos.Domain.Entities;

namespace Pos.Application.Features.Registers;

public static class RegisterMappings
{
    public static readonly Expression<Func<RegisterSession, RegisterRow>> Projection = r => new RegisterRow(
        r.Id,
        r.UserId,
        r.User.FullName,
        r.Status,
        r.OpenedAt,
        r.ClosedAt,
        r.OpeningCash,
        r.ExpectedCash,
        r.ClosingCash,
        r.OpeningNotes,
        r.ClosingNotes);

    public static decimal ExpectedCash(decimal openingCash, RegisterTotals totals) =>
        Money.Round(openingCash + totals.CashSales - totals.CashRefunds - totals.Expenses);

    public static RegisterSessionDto ToDto(this RegisterRow row, RegisterTotals totals)
    {
        // A closed register keeps the expected cash it was closed against.
        var expected = row.ExpectedCash ?? ExpectedCash(row.OpeningCash, totals);
        return new RegisterSessionDto(
            row.Id,
            row.UserId,
            row.UserName,
            row.Status,
            row.OpenedAt,
            row.ClosedAt,
            row.OpeningCash,
            totals.CashSales,
            totals.CardSales,
            totals.OnlineSales,
            totals.CashRefunds,
            totals.Expenses,
            expected,
            row.ClosingCash,
            row.ClosingCash is { } closing ? Money.Round(closing - expected) : null,
            totals.SaleCount,
            row.OpeningNotes,
            row.ClosingNotes);
    }
}
