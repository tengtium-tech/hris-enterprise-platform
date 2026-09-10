using System.Text.Json;
using Hris.Infrastructure.Persistence;
using Hris.Modules.Timekeeping.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Timekeeping.Infrastructure.Persistence;

/// <summary>
/// EF Core configuration for <see cref="WorkSchedule"/> and its owned
/// <see cref="ScheduleAssignment"/> collection.
///
/// Two collections of values without identity — the working-day set and the break
/// rules — map as single JSON columns rather than owned collections, the pattern the
/// Administration module established. Neither has a per-item key, so an owned
/// collection would need a synthetic one that means nothing.
/// </summary>
public sealed class WorkScheduleConfiguration : IEntityTypeConfiguration<WorkSchedule>
{
    public void Configure(EntityTypeBuilder<WorkSchedule> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("work_schedules");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasConversion(new StronglyTypedIdValueConverter<WorkScheduleId>(value => new WorkScheduleId(value)))
            .ValueGeneratedNever();

        builder.Property(s => s.TenantId).IsRequired();
        builder.Property(s => s.LineageId).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(2000);
        builder.Property(s => s.Version).IsRequired();
        builder.Property(s => s.EffectiveFrom).IsRequired();
        builder.Property(s => s.EffectiveTo);
        builder.Property(s => s.Status).IsRequired();
        builder.Property(s => s.CreatedBy).IsRequired();
        builder.Property(s => s.CreatedOn).IsRequired();

        builder.HasIndex(s => new { s.TenantId, s.LineageId });

        var breakComparer = TimekeepingJson.ComparerFor<BreakRule>();

        builder.Property(s => s.BreakPeriods)
            .HasColumnName("break_periods")
            .HasConversion(TimekeepingJson.To<BreakRule>(), TimekeepingJson.From<BreakRule>())
            .Metadata.SetValueComparer(breakComparer);

        builder.OwnsOne(s => s.WorkingDayPattern, pattern =>
        {
            var dayComparer = new ValueComparer<IReadOnlyList<DayOfWeek>>(
                (left, right) => JsonSerializer.Serialize(left, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(right, (JsonSerializerOptions?)null),
                items => JsonSerializer.Serialize(items, (JsonSerializerOptions?)null).GetHashCode(StringComparison.Ordinal),
                items => (IReadOnlyList<DayOfWeek>)items.ToList());

            pattern.Property(p => p.WorkingDays)
                .HasColumnName("working_days")
                .HasConversion(
                    items => JsonSerializer.Serialize(items, (JsonSerializerOptions?)null),
                    json => JsonSerializer.Deserialize<List<DayOfWeek>>(json, (JsonSerializerOptions?)null) ?? new List<DayOfWeek>())
                .Metadata.SetValueComparer(dayComparer);

            // Rest days are derived from working days at construction, so they are not
            // persisted: storing both would allow a stored row to violate the partition
            // invariant the type guarantees in memory.
            pattern.Ignore(p => p.RestDays);
        });
        builder.Navigation(s => s.WorkingDayPattern).UsePropertyAccessMode(PropertyAccessMode.Property).IsRequired();

        builder.OwnsOne(s => s.StandardHours, hours =>
        {
            hours.Property(h => h.Start).HasColumnName("standard_hours_start");
            hours.Property(h => h.End).HasColumnName("standard_hours_end");
        });

        builder.OwnsMany(s => s.ScheduleAssignments, assignment =>
        {
            assignment.ToTable("work_schedule_assignments");
            assignment.WithOwner().HasForeignKey("work_schedule_id");
            assignment.HasKey(a => a.Id);

            assignment.Property(a => a.Id)
                .HasConversion(new StronglyTypedIdValueConverter<ScheduleAssignmentId>(value => new ScheduleAssignmentId(value)))
                .ValueGeneratedNever();

            assignment.Property(a => a.TargetLevel).IsRequired();
            assignment.Property(a => a.TargetId).HasMaxLength(200).IsRequired();
            assignment.Property(a => a.EffectiveFrom).IsRequired();
            assignment.Property(a => a.EffectiveTo);
            assignment.Property(a => a.AssignedBy).IsRequired();
            assignment.Property(a => a.AssignedOn).IsRequired();
        });

        builder.Navigation(s => s.ScheduleAssignments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
