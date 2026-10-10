using Pos.Domain.Common;
using Pos.Domain.Enums;
using Pos.Domain.Exceptions;

namespace Pos.Domain.Entities;

/// <summary>
/// A cash register shift: opened with the cash in hand and closed with the counted cash.
/// </summary>
public class RegisterSession : AuditableEntity
{
    public const int NotesMaxLength = 500;

    private RegisterSession() { }

    public RegisterSession(int userId, decimal openingCash, string? notes, DateTime utcNow)
    {
        UserId = userId;
        OpeningCash = Money.Round(Guard.NotNegative(openingCash, "Opening cash"));
        OpeningNotes = Guard.Optional(notes, "Notes", NotesMaxLength);
        OpenedAt = utcNow;
        Status = RegisterStatus.Open;
    }

    public int UserId { get; private set; }
    public User User { get; private set; } = default!;
    public RegisterStatus Status { get; private set; }
    public DateTime OpenedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public decimal OpeningCash { get; private set; }

    /// <summary>Cash the system expected in the drawer at closing time (snapshot).</summary>
    public decimal? ExpectedCash { get; private set; }

    /// <summary>Cash physically counted at closing time.</summary>
    public decimal? ClosingCash { get; private set; }

    public string? OpeningNotes { get; private set; }
    public string? ClosingNotes { get; private set; }
    public int? ClosedById { get; private set; }

    public bool IsOpen => Status == RegisterStatus.Open;

    public void EnsureOpen()
    {
        if (!IsOpen)
            throw new DomainException("This register is already closed.");
    }

    public void Close(decimal closingCash, decimal expectedCash, string? notes, int closedById, DateTime utcNow)
    {
        EnsureOpen();
        ClosingCash = Money.Round(Guard.NotNegative(closingCash, "Closing cash"));
        ExpectedCash = Money.Round(expectedCash);
        ClosingNotes = Guard.Optional(notes, "Notes", NotesMaxLength);
        ClosedById = closedById;
        ClosedAt = utcNow;
        Status = RegisterStatus.Closed;
    }
}
