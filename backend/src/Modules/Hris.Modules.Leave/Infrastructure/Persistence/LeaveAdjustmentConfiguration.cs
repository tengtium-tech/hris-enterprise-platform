using Hris.Infrastructure.Persistence;
using Hris.Modules.Leave.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Leave.Infrastructure.Persistence;

/// <summary>
/// EF Core configuration for <see cref="LeaveAdjustment"/>. Source:
/// docs/04-modules/leave/domain/aggregates.md (LeaveAdjustment) and
/// infrastructure/persistence.md.
/// </summary>
public sealed class LeaveAdjustmentConfiguration : IEntityTypeConfiguration<LeaveAdjustment>
{
    public void Configure(EntityTypeBuilder<LeaveAdjustment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("leave_adjustments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasConversion(new StronglyTypedIdValueConverter<LeaveAdjustmentId>(value => new LeaveAdjustmentId(value)))
            .ValueGeneratedNever();

        builder.Property(a => a.TenantId).IsRequired();
        builder.Property(a => a.LeaveBalanceId)
            .HasConversion(new StronglyTypedIdValueConverter<LeaveBalanceId>(value => new LeaveBalanceId(value)))
            .IsRequired();
        builder.Property(a => a.OriginalValueSnapshot).HasColumnType("decimal(9,2)").IsRequired();
        builder.Property(a => a.RequestedAmount).HasColumnType("decimal(9,2)").IsRequired();
        builder.Property(a => a.Reason).HasMaxLength(1000).IsRequired();

        builder.Property(a => a.SupportingDocuments)
            .HasColumnName("supporting_documents")
            .HasConversion(LeaveJson.ToValue<IReadOnlyList<string>>(), LeaveJson.FromValue<IReadOnlyList<string>>())
            .Metadata.SetValueComparer(LeaveJson.ValueComparer<IReadOnlyList<string>>());

        builder.Property(a => a.Status).IsRequired();
        builder.Property(a => a.SubmittedBy).IsRequired();
        builder.Property(a => a.SubmittedOn).IsRequired();
        builder.Property(a => a.ReviewerId);
        builder.Property(a => a.ReviewNotes).HasMaxLength(1000);

        builder.Property(a => a.Decision)
            .HasColumnName("decision")
            .HasConversion(LeaveJson.ToValue<ApprovalDecision?>(), LeaveJson.FromValue<ApprovalDecision?>())
            .Metadata.SetValueComparer(LeaveJson.ValueComparer<ApprovalDecision?>());

        builder.Property(a => a.RejectionReason).HasMaxLength(1000);
        builder.Property(a => a.AppliedAt);

        builder.HasIndex(a => new { a.TenantId, a.Status, a.LeaveBalanceId });
    }
}
