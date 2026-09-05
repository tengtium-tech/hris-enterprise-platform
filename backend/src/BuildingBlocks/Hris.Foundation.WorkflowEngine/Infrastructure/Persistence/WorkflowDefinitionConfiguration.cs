using System.Text.Json;
using Hris.Foundation.WorkflowEngine.Domain;
using Hris.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.WorkflowEngine.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="WorkflowDefinition"/> Aggregate
/// Root and its <see cref="WorkflowDefinitionVersion"/> child Entity, per
/// coding-standards.md's Infrastructure Layer convention. <c>OwnsMany</c> with
/// <c>PropertyAccessMode.Field</c> on <see cref="WorkflowDefinition.Versions"/> is the
/// identical shape <c>ConfigurationSettingConfiguration</c>'s own remarks already
/// establish for its own sibling versioned child Entity.
///
/// <see cref="WorkflowDefinitionVersion.Steps"/> is mapped as a single JSON column via
/// <c>HasConversion</c>, not a nested owned collection -- the identical "a converter is
/// simpler than an Owned Type when there is only one underlying column, and no
/// independent per-row query need of its own component parts" choice
/// <c>SearchIndexDefinitionConfiguration</c>'s own remarks already make for
/// <c>SearchIndexDefinition.Fields</c>, applied here inside an <c>OwnsMany</c> builder
/// rather than at the Aggregate Root's own top level -- the same conversion mechanism,
/// regardless of which level configures it. <see cref="WorkflowDefinitionVersion"/>'s
/// own constructor parameter <c>steps</c> already matches <see cref="WorkflowDefinitionVersion.Steps"/>
/// by name, so this child Entity needs no <c>PropertyAccessMode.Field</c> override of
/// its own -- confirmed by a real model build, not assumed.
/// </summary>
public sealed class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(definition => definition.Id);

        builder.Property(definition => definition.Id)
            .HasConversion(new StronglyTypedIdValueConverter<WorkflowDefinitionId>(value => new WorkflowDefinitionId(value)))
            .ValueGeneratedNever();

        builder.Property(definition => definition.TenantId).IsRequired();

        builder.HasIndex(definition => definition.TenantId);

        builder.Property(definition => definition.Name).HasMaxLength(200).IsRequired();

        builder.Property(definition => definition.TriggerType).IsRequired();

        builder.Property(definition => definition.TriggerExpression).HasMaxLength(500);

        builder.Property(definition => definition.CreatedAtUtc).IsRequired();

        builder.OwnsMany(definition => definition.Versions, version =>
        {
            version.WithOwner().HasForeignKey("WorkflowDefinitionId");

            version.HasKey(v => v.Id);

            version.Property(v => v.Id)
                .HasConversion(new StronglyTypedIdValueConverter<WorkflowDefinitionVersionId>(value => new WorkflowDefinitionVersionId(value)))
                .ValueGeneratedNever();

            version.Property(v => v.VersionNumber).IsRequired();

            version.Property(v => v.Steps)
                .HasConversion(
                    steps => JsonSerializer.Serialize(steps, (JsonSerializerOptions?)null),
                    json => JsonSerializer.Deserialize<List<WorkflowStepDefinition>>(json, (JsonSerializerOptions?)null)
                        ?? new List<WorkflowStepDefinition>())
                .IsRequired();

            version.Property(v => v.Status).IsRequired();
            version.Property(v => v.CreatedAtUtc).IsRequired();
            version.Property(v => v.PublishedAtUtc);
        });

        builder.Navigation(definition => definition.Versions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
