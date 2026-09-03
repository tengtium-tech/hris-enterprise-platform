using Hris.Foundation.Scheduling.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.Scheduling.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="Schedule"/> Aggregate Root, per
/// coding-standards.md's Infrastructure Layer convention. No owned type or Complex Type
/// anywhere in this shape -- every property is a scalar, an enum, or a single-column
/// <c>HasConversion</c> Value Object, and <see cref="Schedule"/>'s own constructor
/// avoided the property-name-mismatch pitfall Search Framework's own
/// <c>IndexedDocument</c>/<c>SearchExecution</c>/<c>SavedSearch</c> each needed a
/// second constructor for (see <see cref="Schedule.Create"/>'s own remarks), so this
/// configuration needs none of that extra handling either -- confirmed by a real model
/// build, not assumed.
/// </summary>
public sealed class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
{
    public void Configure(EntityTypeBuilder<Schedule> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(schedule => schedule.Id);

        builder.Property(schedule => schedule.Id)
            .HasConversion(new StronglyTypedIdValueConverter<ScheduleId>(value => new ScheduleId(value)))
            .ValueGeneratedNever();

        builder.Property(schedule => schedule.TenantId).IsRequired();

        builder.HasIndex(schedule => new { schedule.TenantId, schedule.Status });

        builder.Property(schedule => schedule.ScheduleType).IsRequired();

        builder.Property(schedule => schedule.Expression)
            .HasConversion(
                expression => expression.Value,
                value => ScheduleExpression.Create(value).Value)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(schedule => schedule.TimeZone)
            .HasConversion(
                timeZone => timeZone.Value,
                value => ScheduleTimeZone.Create(value).Value)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(schedule => schedule.TaskType).HasMaxLength(200).IsRequired();

        builder.Property(schedule => schedule.TaskReferenceId).HasMaxLength(200);

        builder.Property(schedule => schedule.HolidayBehavior).IsRequired();

        builder.Property(schedule => schedule.CalendarReference).HasMaxLength(200);

        builder.Property(schedule => schedule.Status).IsRequired();

        builder.Property(schedule => schedule.CreatedAtUtc).IsRequired();

        builder.Property(schedule => schedule.LastTransitionAtUtc);
    }
}
