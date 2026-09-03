using Hris.Foundation.Search.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.Search.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="SavedSearch"/> Aggregate Root,
/// per coding-standards.md's Infrastructure Layer convention.
/// </summary>
public sealed class SavedSearchConfiguration : IEntityTypeConfiguration<SavedSearch>
{
    public void Configure(EntityTypeBuilder<SavedSearch> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(savedSearch => savedSearch.Id);

        builder.Property(savedSearch => savedSearch.Id)
            .HasConversion(new StronglyTypedIdValueConverter<SavedSearchId>(value => new SavedSearchId(value)))
            .ValueGeneratedNever();

        builder.Property(savedSearch => savedSearch.TenantId).IsRequired();

        builder.Property(savedSearch => savedSearch.OwnerUserId).IsRequired();

        builder.HasIndex(savedSearch => new { savedSearch.TenantId, savedSearch.OwnerUserId });

        builder.Property(savedSearch => savedSearch.Name).HasMaxLength(200).IsRequired();

        builder.Property(savedSearch => savedSearch.QueryText).HasMaxLength(500).IsRequired();

        builder.Property(savedSearch => savedSearch.DomainFilter).HasMaxLength(100);

        builder.Property(savedSearch => savedSearch.CreatedAtUtc).IsRequired();

        builder.Property(savedSearch => savedSearch.LastSuggestedAtUtc);

        builder.Property(savedSearch => savedSearch.SuggestedCount).IsRequired();
    }
}
