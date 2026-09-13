using Hris.Infrastructure.Persistence;
using Hris.Modules.Attendance.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Attendance.Infrastructure.Persistence;

/// <summary>
/// EF Core configuration for <see cref="BiometricEnrollment"/>. Only the encrypted,
/// non-reversible <see cref="BiometricTemplateReference"/> is stored — never raw biometric
/// bytes (AT-052) — and the synchronized/revocation device sets are JSON lists so
/// revocation completion can be tracked against confirmed devices (AT-053). Source:
/// docs/04-modules/attendance/domain/aggregates.md (BiometricEnrollment).
/// </summary>
public sealed class BiometricEnrollmentConfiguration : IEntityTypeConfiguration<BiometricEnrollment>
{
    public void Configure(EntityTypeBuilder<BiometricEnrollment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("biometric_enrollments");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(new StronglyTypedIdValueConverter<BiometricEnrollmentId>(value => new BiometricEnrollmentId(value)))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.EmployeeId).IsRequired();
        builder.Property(e => e.Method).IsRequired();
        builder.Property(e => e.TemplateReference)
            .HasColumnName("template_reference")
            .HasConversion(AttendanceJson.ToValue<BiometricTemplateReference>(), AttendanceJson.FromValue<BiometricTemplateReference>())
            .Metadata.SetValueComparer(AttendanceJson.ValueComparer<BiometricTemplateReference>());
        builder.Property(e => e.Status).IsRequired();
        builder.Property(e => e.Vendor).HasMaxLength(200);

        builder.Property(e => e.SynchronizedDeviceIds)
            .HasColumnName("synchronized_device_ids")
            .HasConversion(AttendanceJson.To<Guid>(), AttendanceJson.From<Guid>())
            .Metadata.SetValueComparer(AttendanceJson.ComparerFor<Guid>());

        builder.Property(e => e.PendingRevocationDeviceIds)
            .HasColumnName("pending_revocation_device_ids")
            .HasConversion(AttendanceJson.To<Guid>(), AttendanceJson.From<Guid>())
            .Metadata.SetValueComparer(AttendanceJson.ComparerFor<Guid>());

        builder.HasIndex(e => new { e.TenantId, e.EmployeeId });
    }
}
