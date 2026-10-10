using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pos.Domain.Entities;

namespace Pos.Infrastructure.Persistence.Configurations;

internal sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.Property(e => e.Title).HasMaxLength(Expense.TitleMaxLength).IsRequired();
        builder.Property(e => e.Category).HasMaxLength(Expense.CategoryMaxLength);
        builder.Property(e => e.Notes).HasMaxLength(Expense.NotesMaxLength);

        builder.HasOne(e => e.RegisterSession).WithMany().HasForeignKey(e => e.RegisterSessionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.CreatedBy).WithMany().HasForeignKey(e => e.CreatedById).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(e => e.RegisterSessionId);
    }
}
