using Hris.Infrastructure.Persistence;
using Hris.Modules.Attendance.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Attendance.Infrastructure.Persistence;

/// <summary>
/// EF Core configuration for <see cref="AttendanceDevice"/>. Location and operational
/// configuration are stored as JSON value objects; the device's status drives whether it
/// may submit events (AT-050). Source: docs/04-modules/attendance/domain/aggregates.md
/// (AttendanceDevice).
/// </summary>
public sealed class AttendanceDeviceConfiguration : IEntityTypeConfiguration<AttendanceDevice>
{
    public void Configure(EntityTypeBuilder<AttendanceDevice> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("attendance_devices");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasConversion(new StronglyTypedIdValueConverter<AttendanceDeviceId>(value => new AttendanceDeviceId(value)))
            .ValueGeneratedNever();

        builder.Property(d => d.TenantId).IsRequired();
        builder.Property(d => d.Name).HasMaxLength(200).IsRequired();
        builder.Property(d => d.SerialNumber).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Manufacturer).HasMaxLength(200);
        builder.Property(d => d.Model).HasMaxLength(200);
        builder.Property(d => d.FirmwareVersion).HasMaxLength(100);
        builder.Property(d => d.Type).IsRequired();
        builder.Property(d => d.Location)
            .HasColumnName("location")
            .HasConversion(AttendanceJson.ToValue<DeviceLocation>(), AttendanceJson.FromValue<DeviceLocation>())
            .Metadata.SetValueComparer(AttendanceJson.ValueComparer<DeviceLocation>());
        builder.Property(d => d.Configuration)
            .HasColumnName("configuration")
            .HasConversion(AttendanceJson.ToValue<DeviceConfiguration>(), AttendanceJson.FromValue<DeviceConfiguration>())
            .Metadata.SetValueComparer(AttendanceJson.ValueComparer<DeviceConfiguration>());
        builder.Property(d => d.Status).IsRequired();
        builder.Property(d => d.LastSynchronizedUtc);

        builder.HasIndex(d => new { d.TenantId, d.Status });
        builder.HasIndex(d => d.SerialNumber).IsUnique();
    }
}
