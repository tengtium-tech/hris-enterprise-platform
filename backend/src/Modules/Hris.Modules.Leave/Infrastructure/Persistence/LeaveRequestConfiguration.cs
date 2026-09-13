using Hris.Infrastructure.Persistence;
using Hris.Modules.Leave.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Leave.Infrastructure.Persistence;

/// <summary>
/// EF Core configuration for <see cref="LeaveRequest"/>. <see cref="LeaveDateRange"/>,
/// <see cref="StatutoryDetails"/>, and <see cref="ApprovalDecision"/> are JSON columns —
/// the identical shape <c>Hris.Modules.Attendance</c> uses for its own bundled value
/// objects. Source: docs/04-modules/leave/domain/aggregates.md (LeaveRequest) and
/// infrastructure/persistence.md.
/// </summary>
public sealed class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("leave_requests");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasConversion(new StronglyTypedIdValueConverter<LeaveRequestId>(value => new LeaveRequestId(value)))
            .ValueGeneratedNever();

        builder.Property(r => r.TenantId).IsRequired();
        builder.Property(r => r.EmployeeId).IsRequired();
        builder.Property(r => r.LeaveTypeId)
            .HasConversion(new StronglyTypedIdValueConverter<LeaveTypeId>(value => new LeaveTypeId(value)))
            .IsRequired();

        builder.Property(r => r.DateRange)
            .HasColumnName("date_range")
            .HasConversion(LeaveJson.ToValue<LeaveDateRange>(), LeaveJson.FromValue<LeaveDateRange>())
            .Metadata.SetValueComparer(LeaveJson.ValueComparer<LeaveDateRange>());

        builder.Property(r => r.StatutoryDetails)
            .HasColumnName("statutory_details")
            .HasConversion(LeaveJson.ToValue<StatutoryDetails?>(), LeaveJson.FromValue<StatutoryDetails?>())
            .Metadata.SetValueComparer(LeaveJson.ValueComparer<StatutoryDetails?>());

        builder.Property(r => r.SupportingDocuments)
            .HasColumnName("supporting_documents")
            .HasConversion(LeaveJson.ToValue<IReadOnlyList<string>>(), LeaveJson.FromValue<IReadOnlyList<string>>())
            .Metadata.SetValueComparer(LeaveJson.ValueComparer<IReadOnlyList<string>>());

        builder.Property(r => r.Status).IsRequired();
        builder.Property(r => r.PayTreatment);
        builder.Property(r => r.PaidDays).HasColumnType("decimal(9,2)");
        builder.Property(r => r.SubmittedBy).IsRequired();
        builder.Property(r => r.SubmittedOn).IsRequired();

        builder.Property(r => r.Decision)
            .HasColumnName("decision")
            .HasConversion(LeaveJson.ToValue<ApprovalDecision?>(), LeaveJson.FromValue<ApprovalDecision?>())
            .Metadata.SetValueComparer(LeaveJson.ValueComparer<ApprovalDecision?>());

        builder.Property(r => r.RejectionReason).HasMaxLength(1000);
        builder.Property(r => r.CancellationReason).HasMaxLength(1000);

        builder.HasIndex(r => new { r.TenantId, r.EmployeeId, r.Status });
        builder.HasIndex(r => new { r.TenantId, r.Status });
    }
}
