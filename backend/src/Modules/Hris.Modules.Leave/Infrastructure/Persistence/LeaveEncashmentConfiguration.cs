using Hris.Infrastructure.Persistence;
using Hris.Modules.Leave.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Leave.Infrastructure.Persistence;

/// <summary>
/// EF Core configuration for <see cref="LeaveEncashment"/>. Source:
/// docs/04-modules/leave/domain/aggregates.md (LeaveEncashment) and
/// infrastructure/persistence.md.
/// </summary>
public sealed class LeaveEncashmentConfiguration : IEntityTypeConfiguration<LeaveEncashment>
{
    public void Configure(EntityTypeBuilder<LeaveEncashment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("leave_encashments");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(new StronglyTypedIdValueConverter<LeaveEncashmentId>(value => new LeaveEncashmentId(value)))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.EmployeeId).IsRequired();
        builder.Property(e => e.LeaveBalanceId)
            .HasConversion(new StronglyTypedIdValueConverter<LeaveBalanceId>(value => new LeaveBalanceId(value)))
            .IsRequired();
        builder.Property(e => e.RequestedAmount).HasColumnType("decimal(9,2)").IsRequired();

        builder.Property(e => e.Status).IsRequired();
        builder.Property(e => e.SubmittedBy).IsRequired();
        builder.Property(e => e.SubmittedOn).IsRequired();

        builder.Property(e => e.Decision)
            .HasColumnName("decision")
            .HasConversion(LeaveJson.ToValue<ApprovalDecision?>(), LeaveJson.FromValue<ApprovalDecision?>())
            .Metadata.SetValueComparer(LeaveJson.ValueComparer<ApprovalDecision?>());

        builder.Property(e => e.RejectionReason).HasMaxLength(1000);
        builder.Property(e => e.CancellationReason).HasMaxLength(1000);

        builder.HasIndex(e => new { e.TenantId, e.Status, e.LeaveBalanceId });
    }
}
