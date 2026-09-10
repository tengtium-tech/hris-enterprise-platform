using Hris.Infrastructure.Persistence;
using Hris.Modules.Timekeeping.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Timekeeping.Infrastructure.Persistence;

/// <summary>
/// EF Core configuration for <see cref="WorkShift"/>.
///
/// <see cref="ShiftTiming"/> is an owned type holding two further owned
/// <see cref="TimeWindow"/> values, which is the same three-level nesting the
/// Workflow module proved EF Core accepts (root, owned, owned). Both nested windows
/// are optional and mutually exclusive by shift kind, so both map as nullable owned
/// references rather than being flattened into four loose columns that would let a
/// half-populated timing round-trip.
/// </summary>
public sealed class WorkShiftConfiguration : IEntityTypeConfiguration<WorkShift>
{
    public void Configure(EntityTypeBuilder<WorkShift> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("work_shifts");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasConversion(new StronglyTypedIdValueConverter<WorkShiftId>(value => new WorkShiftId(value)))
            .ValueGeneratedNever();

        builder.Property(s => s.TenantId).IsRequired();
        builder.Property(s => s.LineageId).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.IsOvernight).IsRequired();
        builder.Property(s => s.OvertimeEligible).IsRequired();
        builder.Property(s => s.Version).IsRequired();
        builder.Property(s => s.EffectiveFrom).IsRequired();
        builder.Property(s => s.EffectiveTo);
        builder.Property(s => s.Status).IsRequired();
        builder.Property(s => s.CreatedBy).IsRequired();
        builder.Property(s => s.CreatedOn).IsRequired();

        builder.HasIndex(s => new { s.TenantId, s.LineageId });

        builder.OwnsOne(s => s.Code, code =>
        {
            code.Property(c => c.Value).HasColumnName("code").HasMaxLength(ShiftCode.MaximumLength).IsRequired();
            code.HasIndex(c => c.Value);
        });
        builder.Navigation(s => s.Code).UsePropertyAccessMode(PropertyAccessMode.Property).IsRequired();

        builder.Property(s => s.SplitPeriods)
            .HasColumnName("split_periods")
            .HasConversion(TimekeepingJson.To<ShiftPeriod>(), TimekeepingJson.From<ShiftPeriod>())
            .Metadata.SetValueComparer(TimekeepingJson.ComparerFor<ShiftPeriod>());

        builder.Property(s => s.BreakRules)
            .HasColumnName("break_rules")
            .HasConversion(TimekeepingJson.To<BreakRule>(), TimekeepingJson.From<BreakRule>())
            .Metadata.SetValueComparer(TimekeepingJson.ComparerFor<BreakRule>());

        builder.OwnsOne(s => s.Timing, timing =>
        {
            timing.Property(t => t.Kind).HasColumnName("timing_kind").IsRequired();
            timing.Property(t => t.EarliestStart).HasColumnName("timing_earliest_start");
            timing.Property(t => t.LatestStart).HasColumnName("timing_latest_start");
            timing.Property(t => t.RequiredHours).HasColumnName("timing_required_hours");

            timing.OwnsOne(t => t.FixedWindow, window =>
            {
                window.Property(w => w.Start).HasColumnName("timing_fixed_start");
                window.Property(w => w.End).HasColumnName("timing_fixed_end");
            });

            timing.OwnsOne(t => t.CoreHours, window =>
            {
                window.Property(w => w.Start).HasColumnName("timing_core_start");
                window.Property(w => w.End).HasColumnName("timing_core_end");
            });
        });
        builder.Navigation(s => s.Timing).UsePropertyAccessMode(PropertyAccessMode.Property).IsRequired();

        builder.OwnsOne(s => s.AnchorRule, anchor =>
        {
            anchor.Property(a => a.AnchorPoint).HasColumnName("anchor_point");
            anchor.Property(a => a.Description).HasColumnName("anchor_description").HasMaxLength(500);
        });

        builder.OwnsOne(s => s.PremiumEligibility, premium =>
        {
            premium.Property(p => p.NightDifferentialEligible).HasColumnName("night_differential_eligible");
            premium.Property(p => p.HazardEligible).HasColumnName("hazard_eligible");
            premium.Property(p => p.HolidayPremiumEligible).HasColumnName("holiday_premium_eligible");
        });
        builder.Navigation(s => s.PremiumEligibility).UsePropertyAccessMode(PropertyAccessMode.Property).IsRequired();
    }
}
