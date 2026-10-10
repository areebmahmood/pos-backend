using Pos.Domain.Common;

namespace Pos.Domain.Entities;

public class Expense : AuditableEntity
{
    public const int TitleMaxLength = 150;
    public const int CategoryMaxLength = 100;
    public const int NotesMaxLength = 1000;

    private Expense() { }

    public Expense(string title, string? category, decimal amount, string? notes, RegisterSession register, int createdById)
    {
        register.EnsureOpen();
        Title = Guard.NotEmpty(title, "Title", TitleMaxLength);
        Category = Guard.Optional(category, "Category", CategoryMaxLength);
        Amount = Money.Round(Guard.Positive(amount, "Amount"));
        Notes = Guard.Optional(notes, "Notes", NotesMaxLength);
        RegisterSessionId = register.Id;
        CreatedById = createdById;
    }

    public string Title { get; private set; } = default!;
    public string? Category { get; private set; }
    public decimal Amount { get; private set; }
    public string? Notes { get; private set; }
    public int RegisterSessionId { get; private set; }
    public RegisterSession RegisterSession { get; private set; } = default!;
    public int CreatedById { get; private set; }
    public User CreatedBy { get; private set; } = default!;
}
