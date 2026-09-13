using Hris.Infrastructure.Persistence;
using Hris.Modules.Leave.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Leave.Infrastructure.Persistence;

/// <summary>
/// EF Core configuration for <see cref="LeavePolicy"/> and its owned
/// <see cref="PolicyAssignment"/> collection. The <see cref="LeavePolicyRuleset"/> is one
/// JSON column so a revision can carry it unchanged; assignments are an owned collection
/// reached only through the policy (CTR-ARC-004). Source:
/// docs/04-modules/leave/domain/aggregates.md (LeavePolicy) and entities.md
/// (PolicyAssignment).
/// </summary>
public sealed class LeavePolicyConfiguration : IEntityTypeConfiguration<LeavePolicy>
{
    public void Configure(EntityTypeBuilder<LeavePolicy> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("leave_policies");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasConversion(new StronglyTypedIdValueConverter<LeavePolicyId>(value => new LeavePolicyId(value)))
            .ValueGeneratedNever();

        builder.Property(p => p.TenantId).IsRequired();
        builder.Property(p => p.LeaveTypeId)
            .HasConversion(new StronglyTypedIdValueConverter<LeaveTypeId>(value => new LeaveTypeId(value)))
            .IsRequired();
        builder.Property(p => p.LineageId).IsRequired();
        builder.Property(p => p.Version).IsRequired();
        builder.Property(p => p.EffectiveFrom);
        builder.Property(p => p.EffectiveTo);
        builder.Property(p => p.Status).IsRequired();
        builder.Property(p => p.CreatedBy).IsRequired();
        builder.Property(p => p.CreatedOn).IsRequired();

        builder.HasIndex(p => new { p.TenantId, p.LineageId });
        builder.HasIndex(p => new { p.TenantId, p.LeaveTypeId, p.EffectiveFrom, p.EffectiveTo });

        builder.Property(p => p.Ruleset)
            .HasColumnName("ruleset")
            .HasConversion(LeaveJson.ToValue<LeavePolicyRuleset>(), LeaveJson.FromValue<LeavePolicyRuleset>())
            .Metadata.SetValueComparer(LeaveJson.ValueComparer<LeavePolicyRuleset>());

        builder.OwnsMany(p => p.PolicyAssignments, pa =>
        {
            pa.ToTable("leave_policy_assignments");
            pa.WithOwner().HasForeignKey("leave_policy_id");
            pa.HasKey(a => a.Id);

            pa.Property(a => a.Id)
                .HasConversion(new StronglyTypedIdValueConverter<PolicyAssignmentId>(value => new PolicyAssignmentId(value)))
                .ValueGeneratedNever();

            pa.Property(a => a.ScopeLevel).IsRequired();
            pa.Property(a => a.ScopeTargetId).HasMaxLength(200).IsRequired();
            pa.Property(a => a.EffectiveFrom).IsRequired();
            pa.Property(a => a.EffectiveTo);
            pa.Property(a => a.AssignedBy).IsRequired();
            pa.Property(a => a.AssignedOn).IsRequired();
        });
        builder.Navigation(p => p.PolicyAssignments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
