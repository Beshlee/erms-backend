using ERMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERMS.Infrastructure.Persistence.Configurations;

public sealed class ApprovalConfiguration : IEntityTypeConfiguration<Approval>
{
    public void Configure(EntityTypeBuilder<Approval> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Decision)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Comment)
            .HasMaxLength(1000);

        builder.HasOne(x => x.Request)
            .WithMany(x => x.Approvals)
            .HasForeignKey(x => x.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // Aynı User'a birden fazla cascade yolu açılmaması için Restrict.
        builder.HasOne(x => x.Approver)
            .WithMany(x => x.Approvals)
            .HasForeignKey(x => x.ApproverId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
