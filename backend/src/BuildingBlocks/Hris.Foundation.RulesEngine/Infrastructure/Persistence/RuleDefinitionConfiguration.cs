using System.Text.Json;
using Hris.Foundation.Identity.Domain;
using Hris.Foundation.RulesEngine.Domain;
using Hris.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.RulesEngine.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="RuleDefinition"/> Aggregate
/// Root and its owned <see cref="RuleVersion"/> child Entity, per
/// coding-standards.md's Infrastructure Layer convention -- the identical shape
/// <c>ConfigurationSettingConfiguration</c> already establishes for the sibling
/// lifecycle this framework's own Domain layer mirrors.
///
/// One layer deeper than that configuration: each owned <see cref="RuleVersion"/>
/// itself owns two collections of Value Objects (<see cref="RuleVersion.Conditions"/>,
/// <see cref="RuleVersion.Actions"/>), mapped as nested owned collections -- EF Core's
/// own documented support for owned types within owned types, not something this
/// framework's own Domain shape needed to avoid.
///
/// Discovered automatically by <see cref="HrisDbContext.OnModelCreating"/> via
/// <see cref="PersistenceAssemblyRegistry"/>.
/// </summary>
public sealed class RuleDefinitionConfiguration : IEntityTypeConfiguration<RuleDefinition>
{
    public void Configure(EntityTypeBuilder<RuleDefinition> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(definition => definition.Id);

        builder.Property(definition => definition.Id)
            .HasConversion(new StronglyTypedIdValueConverter<RuleDefinitionId>(value => new RuleDefinitionId(value)))
            .ValueGeneratedNever();

        // RuleKey: a single-property Value Object, mapped with a Value Converter --
        // the same choice ConfigurationSettingConfiguration makes for
        // ConfigurationKey, for the same reason (a converter is simpler than an Owned
        // Type when there is only one underlying column).
        builder.Property(definition => definition.Key)
            .HasConversion(
                key => key.Value,
                value => RuleKey.Create(value).Value)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(definition => definition.Key).IsUnique();

        builder.Property(definition => definition.Category).HasMaxLength(200).IsRequired();

        // RuleVersion: a child Entity, never an Aggregate Root of its own -- OwnsMany
        // targeting the public Versions property, with PropertyAccessMode.Field so EF
        // Core reads/writes the private `_versions` backing list, the identical
        // pattern ConfigurationSettingConfiguration uses for ConfigurationVersion.
        builder.OwnsMany(definition => definition.Versions, version =>
        {
            version.WithOwner().HasForeignKey("RuleDefinitionId");

            version.HasKey(v => v.Id);

            version.Property(v => v.Id)
                .HasConversion(new StronglyTypedIdValueConverter<RuleVersionId>(value => new RuleVersionId(value)))
                .ValueGeneratedNever();

            version.Property(v => v.VersionNumber).IsRequired();
            version.Property(v => v.ConditionOperator).IsRequired();
            version.Property(v => v.Priority).IsRequired();

            version.Property(v => v.CreatedByUserId)
                .HasConversion(new StronglyTypedIdValueConverter<UserAccountId>(value => new UserAccountId(value)))
                .IsRequired();

            version.Property(v => v.State).IsRequired();

            // Conditions/Actions: nested owned collections of Value Objects, one
            // level deeper than ConfigurationSettingConfiguration's own single-level
            // OwnsMany -- each still needs its own PropertyAccessMode.Field for the
            // identical backing-field-encapsulation reason.
            version.OwnsMany(v => v.Conditions, condition =>
            {
                condition.WithOwner().HasForeignKey("RuleVersionId");
                condition.Property(c => c.FieldName).HasColumnName("FieldName").HasMaxLength(200).IsRequired();
                condition.Property(c => c.Operator).HasColumnName("Operator").IsRequired();
                condition.Property(c => c.ComparisonValue).HasColumnName("ComparisonValue").HasMaxLength(500).IsRequired();
            });

            version.Navigation(v => v.Conditions).UsePropertyAccessMode(PropertyAccessMode.Field);

            version.OwnsMany(v => v.Actions, action =>
            {
                action.WithOwner().HasForeignKey("RuleVersionId");
                action.Property(a => a.ActionKey).HasColumnName("ActionKey").HasMaxLength(200).IsRequired();

                // Parameters: a dictionary, serialized to JSON -- the identical
                // choice OutboxEntryConfiguration's own remarks make for
                // EventEnvelope.Metadata, for the same reason (EF Core cannot map a
                // dictionary as a relational column directly).
                action.Property(a => a.Parameters)
                    .HasColumnName("Parameters")
                    .HasConversion(
                        parameters => JsonSerializer.Serialize(parameters, (JsonSerializerOptions?)null),
                        json => JsonSerializer.Deserialize<Dictionary<string, string>>(json, (JsonSerializerOptions?)null)
                            ?? new Dictionary<string, string>())
                    .IsRequired();
            });

            version.Navigation(v => v.Actions).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Navigation(definition => definition.Versions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
