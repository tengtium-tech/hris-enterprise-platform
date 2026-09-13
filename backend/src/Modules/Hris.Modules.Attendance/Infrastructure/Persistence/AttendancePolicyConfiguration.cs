using Hris.Infrastructure.Persistence;
using Hris.Modules.Attendance.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Attendance.Infrastructure.Persistence;

/// <summary>
/// EF Core configuration for <see cref="AttendancePolicy"/> and its owned
/// <see cref="PolicyAssignment"/> collection. The full configuration bundle is one JSON
/// column so the calculation engine can consume it unchanged; assignments are an owned
/// collection reached only through the policy (CTR-ARC-004). Source:
/// docs/04-modules/attendance/domain/aggregates.md (AttendancePolicy) and entities.md
/// (PolicyAssignment).
/// </summary>
public sealed class AttendancePolicyConfiguration : IEntityTypeConfiguration<AttendancePolicy>
{
    public void Configure(EntityTypeBuilder<AttendancePolicy> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("attendance_policies");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasConversion(new StronglyTypedIdValueConverter<AttendancePolicyId>(value => new AttendancePolicyId(value)))
            .ValueGeneratedNever();

        builder.Property(p => p.TenantId).IsRequired();
        builder.Property(p => p.LineageId).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Version).IsRequired();
        builder.Property(p => p.EffectiveFrom).IsRequired();
        builder.Property(p => p.EffectiveTo);
        builder.Property(p => p.Status).IsRequired();
        builder.Property(p => p.CreatedBy).IsRequired();
        builder.Property(p => p.CreatedOn).IsRequired();

        builder.HasIndex(p => new { p.TenantId, p.LineageId });

        builder.Property(p => p.Configuration)
            .HasColumnName("configuration")
            .HasConversion(AttendanceJson.ToValue<PolicyCalculationConfiguration>(), AttendanceJson.FromValue<PolicyCalculationConfiguration>())
            .Metadata.SetValueComparer(AttendanceJson.ValueComparer<PolicyCalculationConfiguration>());

        builder.OwnsMany(p => p.PolicyAssignments, pa =>
        {
            pa.ToTable("attendance_policy_assignments");
            pa.WithOwner().HasForeignKey("attendance_policy_id");
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
