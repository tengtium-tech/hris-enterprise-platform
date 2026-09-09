using System.Text.Json;
using Hris.Infrastructure.Persistence;
using Hris.Modules.Workflow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Workflow.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="ApprovalPolicy"/> Aggregate
/// Root. The unique index on tenant is WR-030's singleton expressed in the schema
/// rather than only in the aggregate: a constraint the database itself holds cannot
/// be bypassed by a second code path that forgets to check.
/// </summary>
public sealed class ApprovalPolicyConfiguration : IEntityTypeConfiguration<ApprovalPolicy>
{
    public void Configure(EntityTypeBuilder<ApprovalPolicy> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("approval_policies");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasConversion(new StronglyTypedIdValueConverter<ApprovalPolicyId>(value => new ApprovalPolicyId(value)))
            .ValueGeneratedNever();

        builder.Property(p => p.TenantId).IsRequired();
        builder.HasIndex(p => p.TenantId).IsUnique();

        builder.Property(p => p.CustomDefinitionsPermitted).IsRequired();
        builder.Property(p => p.LastConfiguredBy);
        builder.Property(p => p.LastConfiguredOn);
        builder.Property(p => p.LastConfigurationReason).HasMaxLength(500);
        builder.Property(p => p.CreatedOn).IsRequired();

        var processComparer = new ValueComparer<IReadOnlyList<Guid>>(
            (left, right) => JsonSerializer.Serialize(left, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(right, (JsonSerializerOptions?)null),
            items => JsonSerializer.Serialize(items, (JsonSerializerOptions?)null).GetHashCode(StringComparison.Ordinal),
            items => (IReadOnlyList<Guid>)items.ToList());

        var limitComparer = new ValueComparer<IReadOnlyList<ApprovalAuthorityLimit>>(
            (left, right) => JsonSerializer.Serialize(left, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(right, (JsonSerializerOptions?)null),
            items => JsonSerializer.Serialize(items, (JsonSerializerOptions?)null).GetHashCode(StringComparison.Ordinal),
            items => (IReadOnlyList<ApprovalAuthorityLimit>)items.ToList());

        builder.Property(p => p.ProcessesRequiringApproval)
            .HasColumnName("processes_requiring_approval")
            .HasConversion(
                items => JsonSerializer.Serialize(items, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<Guid>>(json, (JsonSerializerOptions?)null) ?? new List<Guid>())
            .Metadata.SetValueComparer(processComparer);

        builder.Property(p => p.AuthorityLimits)
            .HasColumnName("authority_limits")
            .HasConversion(
                items => JsonSerializer.Serialize(items, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<ApprovalAuthorityLimit>>(json, (JsonSerializerOptions?)null) ?? new List<ApprovalAuthorityLimit>())
            .Metadata.SetValueComparer(limitComparer);

        builder.OwnsOne(p => p.DefaultEscalation, escalation =>
        {
            escalation.Property(e => e.TriggerAfter).HasColumnName("default_escalation_trigger_after");
            escalation.Property(e => e.MaxDepth).HasColumnName("default_escalation_max_depth");
            escalation.OwnsOne(e => e.EscalateTo, target =>
            {
                target.Property(t => t.Kind).HasColumnName("default_escalation_target_kind");
                target.Property(t => t.RoleName).HasColumnName("default_escalation_target_role_name").HasMaxLength(200);
                target.Property(t => t.ScopeId).HasColumnName("default_escalation_target_scope_id");
            });
        });

        builder.OwnsOne(p => p.DefaultSla, sla =>
        {
            sla.Property(s => s.Value).HasColumnName("default_sla");
        });
    }
}
