using Hris.Infrastructure.Persistence;
using Hris.Modules.Attendance.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Attendance.Infrastructure.Persistence;

/// <summary>
/// EF Core configuration for <see cref="OvertimeRequest"/>. Source:
/// docs/04-modules/attendance/domain/aggregates.md (OvertimeRequest).
/// </summary>
public sealed class OvertimeRequestConfiguration : IEntityTypeConfiguration<OvertimeRequest>
{
    public void Configure(EntityTypeBuilder<OvertimeRequest> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("overtime_requests");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasConversion(new StronglyTypedIdValueConverter<OvertimeRequestId>(value => new OvertimeRequestId(value)))
            .ValueGeneratedNever();

        builder.Property(r => r.TenantId).IsRequired();
        builder.Property(r => r.EmployeeId).IsRequired();
        builder.Property(r => r.WorkDate).IsRequired();
        builder.Property(r => r.PlannedStart);
        builder.Property(r => r.PlannedEnd);
        builder.Property(r => r.EstimatedHours).IsRequired();
        builder.Property(r => r.Category).IsRequired();
        builder.Property(r => r.Justification).HasMaxLength(2000);
        builder.Property(r => r.Status).IsRequired();
        builder.Property(r => r.ApproverId);
        builder.Property(r => r.DecidedOn);
        builder.Property(r => r.RejectionReason).HasMaxLength(2000);

        builder.HasIndex(r => new { r.TenantId, r.EmployeeId, r.WorkDate });
    }
}
