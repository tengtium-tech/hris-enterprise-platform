using Hris.Foundation.DocumentManagement.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.DocumentManagement.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="Document"/> Aggregate Root and
/// its owned <see cref="DocumentVersion"/> child Entity, per coding-standards.md's
/// Infrastructure Layer convention -- the same <c>OwnsMany</c>-with-
/// <c>PropertyAccessMode.Field</c> shape <c>StoredFileConfiguration</c> already
/// establishes for <c>FileVersion</c>.
///
/// <see cref="Document.Tags"/> is mapped through a single delimited-string column
/// conversion, with an explicit <see cref="ValueComparer{T}"/> so EF Core's own change
/// tracking compares the collection's own contents rather than reference identity --
/// without one, replacing <see cref="Document.Tags"/> with an equal-content-but-new
/// list (every call to <c>UpdateMetadata</c>) would be silently invisible to change
/// tracking. The join character is the ASCII Unit Separator (<c>0x1F</c>), spelled out
/// as the <c>\u001F</c> escape rather than a literal control character in source, so
/// the separator stays visible and unambiguous in this file -- a comma is a plausible
/// character within a tag itself, so it cannot be the delimiter.
///
/// Discovered automatically by <c>HrisDbContext.OnModelCreating</c> via
/// <c>PersistenceAssemblyRegistry</c>.
/// </summary>
public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    private const char _tagSeparator = '\u001F';

    public void Configure(EntityTypeBuilder<Document> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(document => document.Id);

        builder.Property(document => document.Id)
            .HasConversion(new StronglyTypedIdValueConverter<DocumentId>(value => new DocumentId(value)))
            .ValueGeneratedNever();

        builder.Property(document => document.TenantId).IsRequired();
        builder.HasIndex(document => document.TenantId);

        builder.Property(document => document.DocumentNumber).HasMaxLength(100);

        builder.Property(document => document.Title).HasMaxLength(260).IsRequired();

        builder.Property(document => document.Category).HasMaxLength(100).IsRequired();
        builder.HasIndex(document => new { document.TenantId, document.Category });

        builder.Property(document => document.Classification).IsRequired();

        builder.Property(document => document.OwnerUserId).IsRequired();

        builder.Property(document => document.CreatedAtUtc).IsRequired();

        builder.Property(document => document.Status).IsRequired();

        var tagsProperty = builder.Property(document => document.Tags)
            .HasConversion(
                tags => string.Join(_tagSeparator, tags),
                value => string.IsNullOrEmpty(value)
                    ? new List<string>()
                    : value.Split(_tagSeparator, StringSplitOptions.RemoveEmptyEntries).ToList())
            .HasColumnName("tags");

        tagsProperty.Metadata.SetValueComparer(new ValueComparer<IReadOnlyList<string>>(
            (left, right) => (left ?? new List<string>()).SequenceEqual(right ?? new List<string>()),
            tags => tags.Aggregate(0, (hash, tag) => HashCode.Combine(hash, tag.GetHashCode(StringComparison.Ordinal))),
            tags => tags.ToList()));

        builder.OwnsMany(document => document.Versions, version => ConfigureVersion(version));
        builder.Navigation(document => document.Versions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureVersion(OwnedNavigationBuilder<Document, DocumentVersion> version)
    {
        version.ToTable("document_versions");
        version.WithOwner().HasForeignKey("DocumentId");

        version.HasKey(v => v.Id);

        version.Property(v => v.Id)
            .HasConversion(new StronglyTypedIdValueConverter<DocumentVersionId>(value => new DocumentVersionId(value)))
            .ValueGeneratedNever();

        version.Property(v => v.MajorVersion).IsRequired();
        version.Property(v => v.MinorVersion).IsRequired();
        version.Property(v => v.StoredFileId).IsRequired();
        version.Property(v => v.CreatedByUserId).IsRequired();
        version.Property(v => v.CreatedAtUtc).IsRequired();
        version.Property(v => v.ChangeSummary).HasMaxLength(2000);
        version.Property(v => v.Status).IsRequired();
    }
}
