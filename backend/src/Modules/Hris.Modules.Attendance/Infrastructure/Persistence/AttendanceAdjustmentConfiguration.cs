using Hris.Infrastructure.Persistence;
using Hris.Modules.Attendance.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Attendance.Infrastructure.Persistence;

/// <summary>
/// EF Core configuration for <see cref="AttendanceAdjustment"/>. The original value is
/// snapshotted (AT-020) so the <c>ApprovalDecision</c> — preserved in full, including any
/// delegated-approver identity (AT-033) — and the supporting documents live alongside it.
/// Source: docs/04-modules/attendance/domain/aggregates.md (AttendanceAdjustment).
/// </summary>
public sealed class AttendanceAdjustmentConfiguration : IEntityTypeConfiguration<AttendanceAdjustment>
{
    public void Configure(EntityTypeBuilder<AttendanceAdjustment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("attendance_adjustments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasConversion(new StronglyTypedIdValueConverter<AttendanceAdjustmentId>(value => new AttendanceAdjustmentId(value)))
            .ValueGeneratedNever();

        builder.Property(a => a.TenantId).IsRequired();
        builder.Property(a => a.AttendanceRecordId)
            .HasConversion(new StronglyTypedIdValueConverter<AttendanceRecordId>(value => new AttendanceRecordId(value)))
            .IsRequired();
        builder.Property(a => a.WorkDate).IsRequired();
        builder.Property(a => a.Field).HasMaxLength(100).IsRequired();
        builder.Property(a => a.OriginalValue).HasMaxLength(2000);
        builder.Property(a => a.RequestedValue).HasMaxLength(2000);
        builder.Property(a => a.Category).IsRequired();
        builder.Property(a => a.Reason).HasMaxLength(2000);
        builder.Property(a => a.SubmittedBy).IsRequired();
        builder.Property(a => a.SubmittedOn).IsRequired();
        builder.Property(a => a.Status).IsRequired();
        builder.Property(a => a.ReviewNotes).HasMaxLength(2000);
        builder.Property(a => a.ReviewerId);
        builder.Property(a => a.ApproverId);
        builder.Property(a => a.Decision)
            .HasColumnName("decision")
            .HasConversion(AttendanceJson.ToNullable<ApprovalDecision>(), AttendanceJson.FromNullable<ApprovalDecision>())
            .Metadata.SetValueComparer(AttendanceJson.NullableComparer<ApprovalDecision>());
        builder.Property(a => a.ApprovedOn);

        builder.HasIndex(a => new { a.TenantId, a.AttendanceRecordId });

        builder.Property(a => a.SupportingDocuments)
            .HasColumnName("supporting_documents")
            .HasConversion(AttendanceJson.To<string>(), AttendanceJson.From<string>())
            .Metadata.SetValueComparer(AttendanceJson.ComparerFor<string>());
    }
}
