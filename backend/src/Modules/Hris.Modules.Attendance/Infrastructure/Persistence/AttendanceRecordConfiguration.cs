using Hris.Infrastructure.Persistence;
using Hris.Modules.Attendance.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Attendance.Infrastructure.Persistence;

/// <summary>
/// EF Core configuration for <see cref="AttendanceRecord"/>, its owned
/// <see cref="TimeEvent"/> collection, and its derived/JSON state. Source:
/// docs/04-modules/attendance/domain/aggregates.md (AttendanceRecord) and entities.md
/// (TimeEvent). The calculated fields and the applied-adjustment history are stored as
/// JSON so a recalculation can always overwrite them in full (AT-003), and the immutable
/// punch set is an owned collection reached only through the record (CTR-ARC-004).
/// </summary>
public sealed class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("attendance_records");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasConversion(new StronglyTypedIdValueConverter<AttendanceRecordId>(value => new AttendanceRecordId(value)))
            .ValueGeneratedNever();

        builder.Property(r => r.TenantId).IsRequired();
        builder.Property(r => r.EmployeeId).IsRequired();
        builder.Property(r => r.WorkDate).IsRequired();
        builder.Property(r => r.WorkShiftId);
        builder.Property(r => r.HolidayCalendarId);
        builder.Property(r => r.Status).IsRequired();
        builder.Property(r => r.ApprovalStatus).IsRequired();
        builder.Property(r => r.PayrollStatus).IsRequired();
        builder.Property(r => r.PendingAdjustmentCount).IsRequired();

        builder.HasIndex(r => new { r.TenantId, r.EmployeeId, r.WorkDate });

        builder.Property(r => r.Calculated)
            .HasColumnName("calculated_fields")
            .HasConversion(AttendanceJson.ToNullable<CalculatedFields>(), AttendanceJson.FromNullable<CalculatedFields>())
            .Metadata.SetValueComparer(AttendanceJson.NullableComparer<CalculatedFields>());

        builder.Property(r => r.Exceptions)
            .HasColumnName("exceptions")
            .HasConversion(AttendanceJson.To<string>(), AttendanceJson.From<string>())
            .Metadata.SetValueComparer(AttendanceJson.ComparerFor<string>());

        builder.Property(r => r.AppliedAdjustments)
            .HasColumnName("applied_adjustments")
            .HasConversion(AttendanceJson.To<AppliedAdjustment>(), AttendanceJson.From<AppliedAdjustment>())
            .Metadata.SetValueComparer(AttendanceJson.ComparerFor<AppliedAdjustment>());

        builder.OwnsMany(r => r.TimeEvents, te =>
        {
            te.ToTable("attendance_record_time_events");
            te.WithOwner().HasForeignKey("attendance_record_id");
            te.HasKey(t => t.Id);

            te.Property(t => t.Id)
                .HasConversion(new StronglyTypedIdValueConverter<TimeEventId>(value => new TimeEventId(value)))
                .ValueGeneratedNever();

            te.Property(t => t.EventType).IsRequired();
            te.Property(t => t.TimestampUtc).IsRequired();
            te.Property(t => t.Source).IsRequired();
            te.Property(t => t.AttendanceDeviceId);
            te.Property(t => t.RawValue);
            te.Property(t => t.OriginatingTimeZone);
        });
        builder.Navigation(r => r.TimeEvents).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
