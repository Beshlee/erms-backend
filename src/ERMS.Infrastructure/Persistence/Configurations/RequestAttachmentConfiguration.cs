using ERMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERMS.Infrastructure.Persistence.Configurations;

public sealed class RequestAttachmentConfiguration : IEntityTypeConfiguration<RequestAttachment>
{
    public void Configure(EntityTypeBuilder<RequestAttachment> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName)
            .HasMaxLength(260)
            .IsRequired();

        builder.Property(x => x.FilePath)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasOne(x => x.Request)
            .WithMany(x => x.Attachments)
            .HasForeignKey(x => x.RequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
