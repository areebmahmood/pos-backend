using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pos.Domain.Entities;
using Pos.Domain.Enums;

namespace Pos.Infrastructure.Persistence.Configurations;

internal sealed class RegisterSessionConfiguration : IEntityTypeConfiguration<RegisterSession>
{
    public void Configure(EntityTypeBuilder<RegisterSession> builder)
    {
        builder.Property(r => r.OpeningNotes).HasMaxLength(RegisterSession.NotesMaxLength);
        builder.Property(r => r.ClosingNotes).HasMaxLength(RegisterSession.NotesMaxLength);
        builder.Ignore(r => r.IsOpen);

        builder.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.ClosedById).OnDelete(DeleteBehavior.Restrict);

        // At most one open register per user, enforced by the database.
        builder.HasIndex(r => r.UserId).IsUnique().HasFilter($"[Status] = {(int)RegisterStatus.Open}")
            .HasDatabaseName("IX_RegisterSessions_UserId_Open");
        builder.HasIndex(r => new { r.UserId, r.OpenedAt });
        builder.HasIndex(r => r.Status);
    }
}
