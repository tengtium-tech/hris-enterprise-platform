using Hris.Infrastructure.Persistence;
using Hris.Modules.Timekeeping.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Timekeeping.Infrastructure.Persistence;

/// <summary>
/// EF Core configuration for <see cref="ShiftAssignment"/>.
///
/// Its own table and its own root, unlike <see cref="ScheduleAssignment"/> which
/// lives inside <see cref="WorkSchedule"/>. The index on tenant and target is what
/// makes the resolver's candidate query cheap: resolution asks for every assignment
/// naming one target across all precedence levels, and that read happens for every
/// employee on every evaluated date once <c>attendance</c> exists.
/// </summary>
public sealed class ShiftAssignmentConfiguration : IEntityTypeConfiguration<ShiftAssignment>
{
    public void Configure(EntityTypeBuilder<ShiftAssignment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("shift_assignments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasConversion(new StronglyTypedIdValueConverter<ShiftAssignmentId>(value => new ShiftAssignmentId(value)))
            .ValueGeneratedNever();

        builder.Property(a => a.TenantId).IsRequired();
        builder.Property(a => a.TargetType).IsRequired();
        builder.Property(a => a.TargetId).HasMaxLength(200).IsRequired();
        builder.Property(a => a.TargetLevel).IsRequired();

        builder.Property(a => a.WorkShiftId)
            .HasConversion(new StronglyTypedIdValueConverter<WorkShiftId>(value => new WorkShiftId(value)))
            .IsRequired();

        builder.Property(a => a.EffectiveFrom).IsRequired();
        builder.Property(a => a.EffectiveTo);
        builder.Property(a => a.Status).IsRequired();

        builder.Property(a => a.RotationCycleReference)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? null : new RotationCycleId(value.Value));

        builder.Property(a => a.SwapLinkedAssignmentId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? null : new ShiftAssignmentId(value.Value));

        builder.Property(a => a.AssignedBy);
        builder.Property(a => a.AssignedOn).IsRequired();

        builder.OwnsOne(a => a.PendingSwap, swap =>
        {
            swap.Property(s => s.CounterpartAssignmentId)
                .HasColumnName("pending_swap_counterpart_id")
                .HasConversion(new StronglyTypedIdValueConverter<ShiftAssignmentId>(value => new ShiftAssignmentId(value)));

            swap.Property(s => s.ProposedEffectiveFrom).HasColumnName("pending_swap_effective_from");
            swap.Property(s => s.InitiatedBy).HasColumnName("pending_swap_initiated_by");
            swap.Property(s => s.PrimaryEmployeeId).HasColumnName("pending_swap_primary_employee_id").HasMaxLength(200);
            swap.Property(s => s.CounterpartEmployeeId).HasColumnName("pending_swap_counterpart_employee_id").HasMaxLength(200);
            swap.Property(s => s.PrimaryConsented).HasColumnName("pending_swap_primary_consented");
            swap.Property(s => s.CounterpartConsented).HasColumnName("pending_swap_counterpart_consented");
            swap.Property(s => s.OverriddenBy).HasColumnName("pending_swap_overridden_by");
            swap.Property(s => s.OverrideReason).HasColumnName("pending_swap_override_reason").HasMaxLength(500);
            swap.Ignore(s => s.IsReadyToActivate);
        });

        builder.HasIndex(a => new { a.TenantId, a.TargetId });
        builder.HasIndex(a => new { a.TenantId, a.Status, a.EffectiveTo });
    }
}
