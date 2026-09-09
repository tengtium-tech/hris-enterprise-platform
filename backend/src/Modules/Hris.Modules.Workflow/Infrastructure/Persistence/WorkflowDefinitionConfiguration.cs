using System.Text.Json;
using Hris.Infrastructure.Persistence;
using Hris.Modules.Workflow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Workflow.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="WorkflowDefinition"/>
/// Aggregate Root and its owned <see cref="WorkflowStep"/> collection.
///
/// Nesting is kept to the one depth this codebase has proven: root aggregate, owned
/// collection, one nested owned value. A step's five owned values sit at that
/// nested level. Two step properties that would otherwise need a third level, its
/// successor list and its condition's branch list, are instead mapped as single
/// JSON-serialized columns, the pattern the Administration module established for
/// values with no identity of their own. A value comparer based on the serialized
/// JSON accompanies each conversion, since EF Core cannot compare two deserialized
/// list instances by reference for change tracking.
/// </summary>
public sealed class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("workflow_definitions");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasConversion(new StronglyTypedIdValueConverter<WorkflowDefinitionId>(value => new WorkflowDefinitionId(value)))
            .ValueGeneratedNever();

        builder.Property(d => d.TenantId).IsRequired();
        builder.Property(d => d.BusinessProcessId).IsRequired();
        builder.Property(d => d.LineageId).IsRequired();
        builder.Property(d => d.Name).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Description).HasMaxLength(2000);
        builder.Property(d => d.TriggerCondition).HasMaxLength(2000);
        builder.Property(d => d.Version).IsRequired();
        builder.Property(d => d.Status).IsRequired();
        builder.Property(d => d.CreatedBy).IsRequired();
        builder.Property(d => d.CreatedOn).IsRequired();
        builder.Property(d => d.PublishedBy);
        builder.Property(d => d.PublishedOn);
        builder.Property(d => d.DeprecatedBy);
        builder.Property(d => d.DeprecatedOn);
        builder.Property(d => d.DeprecationReason).HasMaxLength(500);

        builder.HasIndex(d => new { d.TenantId, d.BusinessProcessId });
        builder.HasIndex(d => d.LineageId);

        var successorComparer = new ValueComparer<IReadOnlyList<Guid>>(
            (left, right) => JsonSerializer.Serialize(left, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(right, (JsonSerializerOptions?)null),
            items => JsonSerializer.Serialize(items, (JsonSerializerOptions?)null).GetHashCode(StringComparison.Ordinal),
            items => (IReadOnlyList<Guid>)items.ToList());

        var branchComparer = new ValueComparer<IReadOnlyList<ConditionBranch>>(
            (left, right) => JsonSerializer.Serialize(left, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(right, (JsonSerializerOptions?)null),
            items => JsonSerializer.Serialize(items, (JsonSerializerOptions?)null).GetHashCode(StringComparison.Ordinal),
            items => (IReadOnlyList<ConditionBranch>)items.ToList());

        builder.OwnsMany(d => d.Steps, step =>
        {
            step.ToTable("workflow_definition_steps");
            step.WithOwner().HasForeignKey("workflow_definition_id");
            step.HasKey(s => s.Id);

            step.Property(s => s.Id)
                .HasConversion(new StronglyTypedIdValueConverter<WorkflowStepId>(value => new WorkflowStepId(value)))
                .ValueGeneratedNever();

            step.Property(s => s.Order).HasColumnName("step_order").IsRequired();
            step.Property(s => s.StepType).IsRequired();
            step.Property(s => s.Name).HasMaxLength(200).IsRequired();
            step.Property(s => s.DefaultSuccessor);
            step.Property(s => s.JoinMode);
            step.Property(s => s.NonDelegable).IsRequired();

            step.Property(s => s.Successors)
                .HasColumnName("successors")
                .HasConversion(
                    items => JsonSerializer.Serialize(items, (JsonSerializerOptions?)null),
                    json => JsonSerializer.Deserialize<List<Guid>>(json, (JsonSerializerOptions?)null) ?? new List<Guid>())
                .Metadata.SetValueComparer(successorComparer);

            step.OwnsOne(s => s.ApproverResolutionRule, rule =>
            {
                rule.Property(r => r.Kind).HasColumnName("approver_resolution_kind");
                rule.Property(r => r.RoleName).HasColumnName("approver_role_name").HasMaxLength(200);
                rule.Property(r => r.ScopeId).HasColumnName("approver_scope_id");
            });

            step.OwnsOne(s => s.CommandReference, command =>
            {
                command.Property(c => c.ModuleName).HasColumnName("command_module_name").HasMaxLength(200);
                command.Property(c => c.CommandId).HasColumnName("command_id").HasMaxLength(200);
            });

            step.OwnsOne(s => s.Condition, condition =>
            {
                condition.Property(c => c.Expression).HasColumnName("condition_expression").HasMaxLength(2000);
                condition.Property(c => c.Branches)
                    .HasColumnName("condition_branches")
                    .HasConversion(
                        items => JsonSerializer.Serialize(items, (JsonSerializerOptions?)null),
                        json => JsonSerializer.Deserialize<List<ConditionBranch>>(json, (JsonSerializerOptions?)null) ?? new List<ConditionBranch>())
                    .Metadata.SetValueComparer(branchComparer);
            });

            step.OwnsOne(s => s.EscalationOverride, escalation =>
            {
                escalation.Property(e => e.TriggerAfter).HasColumnName("escalation_trigger_after");
                escalation.Property(e => e.MaxDepth).HasColumnName("escalation_max_depth");
                escalation.OwnsOne(e => e.EscalateTo, target =>
                {
                    target.Property(t => t.Kind).HasColumnName("escalation_target_kind");
                    target.Property(t => t.RoleName).HasColumnName("escalation_target_role_name").HasMaxLength(200);
                    target.Property(t => t.ScopeId).HasColumnName("escalation_target_scope_id");
                });
            });

            step.OwnsOne(s => s.SlaOverride, sla =>
            {
                sla.Property(s2 => s2.Value).HasColumnName("sla_override");
            });
        });

        builder.Navigation(d => d.Steps).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
