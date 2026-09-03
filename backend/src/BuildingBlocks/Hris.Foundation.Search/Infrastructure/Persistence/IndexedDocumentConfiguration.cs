using Hris.Foundation.Search.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.Search.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="IndexedDocument"/> Aggregate
/// Root, per coding-standards.md's Infrastructure Layer convention. The composite index
/// below backs both <see cref="IIndexedDocumentRepository.FindBySourceAsync"/>'s own
/// lookup and <see cref="IIndexedDocumentRepository.SearchAsync"/>'s own raw-SQL
/// <c>WHERE tenant_id = ...</c> filter -- <see cref="TenantId"/> leads the index for
/// exactly that reason (<c>CTR-ISO-001</c>).
/// </summary>
public sealed class IndexedDocumentConfiguration : IEntityTypeConfiguration<IndexedDocument>
{
    public void Configure(EntityTypeBuilder<IndexedDocument> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(document => document.Id);

        builder.Property(document => document.Id)
            .HasConversion(new StronglyTypedIdValueConverter<IndexedDocumentId>(value => new IndexedDocumentId(value)))
            .ValueGeneratedNever();

        builder.Property(document => document.SearchIndexDefinitionId)
            .HasConversion(new StronglyTypedIdValueConverter<SearchIndexDefinitionId>(value => new SearchIndexDefinitionId(value)))
            .IsRequired();

        builder.Property(document => document.TenantId).IsRequired();

        builder.Property(document => document.SourceEntityType)
            .HasConversion(
                entityType => entityType.Value,
                value => SearchEntityType.Create(value).Value)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(document => document.SourceEntityId).HasMaxLength(200).IsRequired();

        builder.HasIndex(document => new { document.TenantId, document.SourceEntityType, document.SourceEntityId })
            .IsUnique();

        builder.Property(document => document.SearchableContent).IsRequired();

        builder.Property(document => document.SecurityScopeToken).HasMaxLength(200);

        builder.Property(document => document.Status).IsRequired();

        builder.Property(document => document.IndexedAtUtc).IsRequired();

        builder.Property(document => document.LastUpdatedAtUtc).IsRequired();
    }
}
