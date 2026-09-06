using Hris.Foundation.DocumentManagement.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.DocumentManagement.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="DocumentAttachment"/> Aggregate
/// Root, per coding-standards.md's Infrastructure Layer convention. A plain top-level
/// table, not an owned type of <see cref="Document"/> -- see
/// <see cref="DocumentAttachment"/>'s own remarks for why it is its own Aggregate Root.
///
/// Discovered automatically by <c>HrisDbContext.OnModelCreating</c> via
/// <c>PersistenceAssemblyRegistry</c>.
/// </summary>
public sealed class DocumentAttachmentConfiguration : IEntityTypeConfiguration<DocumentAttachment>
{
    public void Configure(EntityTypeBuilder<DocumentAttachment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(attachment => attachment.Id);

        builder.Property(attachment => attachment.Id)
            .HasConversion(new StronglyTypedIdValueConverter<DocumentAttachmentId>(value => new DocumentAttachmentId(value)))
            .ValueGeneratedNever();

        builder.Property(attachment => attachment.DocumentId)
            .HasConversion(new StronglyTypedIdValueConverter<DocumentId>(value => new DocumentId(value)))
            .IsRequired();

        builder.Property(attachment => attachment.TenantId).IsRequired();

        builder.Property(attachment => attachment.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(attachment => attachment.EntityId).IsRequired();

        builder.HasIndex(attachment => new { attachment.TenantId, attachment.EntityType, attachment.EntityId });
        builder.HasIndex(attachment => attachment.DocumentId);

        builder.Property(attachment => attachment.AttachedByUserId).IsRequired();
        builder.Property(attachment => attachment.AttachedAtUtc).IsRequired();
        builder.Property(attachment => attachment.Status).IsRequired();
    }
}
