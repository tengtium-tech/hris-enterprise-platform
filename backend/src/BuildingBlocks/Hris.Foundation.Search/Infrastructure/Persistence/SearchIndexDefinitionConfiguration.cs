using System.Text.Json;
using Hris.Foundation.Search.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.Search.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="SearchIndexDefinition"/>
/// Aggregate Root, per coding-standards.md's Infrastructure Layer convention.
///
/// <see cref="SearchIndexDefinition.Fields"/> is mapped as a single JSON column via
/// <c>HasConversion</c>, not <c>OwnsMany</c> -- the identical "a converter is simpler
/// than an Owned Type when there is only one underlying column, and no independent
/// per-row query need of its own component parts" choice
/// <c>RuleDefinitionConfiguration</c>'s own <c>Parameters</c> dictionary already makes.
/// No <c>ValueComparer</c> is registered, matching that same precedent -- this
/// property is always replaced wholesale by <see cref="SearchIndexDefinition.UpdateFields"/>,
/// never mutated element-by-element in place, so EF Core's default reference-equality
/// change detection already sees every real change.
/// </summary>
public sealed class SearchIndexDefinitionConfiguration : IEntityTypeConfiguration<SearchIndexDefinition>
{
    public void Configure(EntityTypeBuilder<SearchIndexDefinition> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(definition => definition.Id);

        builder.Property(definition => definition.Id)
            .HasConversion(new StronglyTypedIdValueConverter<SearchIndexDefinitionId>(value => new SearchIndexDefinitionId(value)))
            .ValueGeneratedNever();

        builder.Property(definition => definition.EntityType)
            .HasConversion(
                entityType => entityType.Value,
                value => SearchEntityType.Create(value).Value)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(definition => definition.EntityType).IsUnique();

        builder.Property(definition => definition.Fields)
            .HasConversion(
                fields => JsonSerializer.Serialize(fields, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<SearchFieldDefinition>>(json, (JsonSerializerOptions?)null)
                    ?? new List<SearchFieldDefinition>())
            .IsRequired();

        builder.Property(definition => definition.SecurityScopeKey).HasMaxLength(200);

        builder.Property(definition => definition.RegisteredAtUtc).IsRequired();

        builder.Property(definition => definition.LastRebuiltAtUtc);
    }
}
