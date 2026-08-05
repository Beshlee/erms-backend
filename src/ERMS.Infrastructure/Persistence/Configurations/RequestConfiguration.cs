using ERMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERMS.Infrastructure.Persistence.Configurations;

public sealed class RequestConfiguration : IEntityTypeConfiguration<Request>
{
    public void Configure(EntityTypeBuilder<Request> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Priority)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Masraf tutarı (FR-19) — para birimi için yeterli hassasiyet.
        builder.Property(x => x.Amount)
            .HasPrecision(18, 2);

        builder.HasOne(x => x.RequestType)
            .WithMany(x => x.Requests)
            .HasForeignKey(x => x.RequestTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Requester)
            .WithMany(x => x.Requests)
            .HasForeignKey(x => x.RequesterId)
            .OnDelete(DeleteBehavior.Restrict);

        // Filtre/arama/sayfalama performansı için (FR-27, FR-29).
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Title);
    }
}
