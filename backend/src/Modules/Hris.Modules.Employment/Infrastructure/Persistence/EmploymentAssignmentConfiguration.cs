using Hris.Infrastructure.Persistence;
using Hris.Modules.Employment.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Employment.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="EmploymentAssignment"/>
/// Aggregate Root and its own owned child collections (AssignmentHistoryRecord,
/// ReportingAssignment), per infrastructure/persistence.md's own Table Mapping
/// section. A partial unique index enforces "one current EmploymentAssignment per
/// Employment" (ASG-003) -- <see cref="EmploymentAssignment.IsEnded"/> is <c>false</c>
/// for exactly the current instance. Discovered automatically by
/// <c>HrisDbContext.OnModelCreating</c> via <c>PersistenceAssemblyRegistry</c>.
/// </summary>
public sealed class EmploymentAssignmentConfiguration : IEntityTypeConfiguration<EmploymentAssignment>
{
    public void Configure(EntityTypeBuilder<EmploymentAssignment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("employment_assignments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasConversion(
                new StronglyTypedIdValueConverter<EmploymentAssignmentId>(value => new EmploymentAssignmentId(value)))
            .ValueGeneratedNever();

        builder.Property(a => a.TenantId).IsRequired();
        builder.Property(a => a.EmploymentId).IsRequired();

        builder.HasIndex(a => new { a.EmploymentId, a.IsEnded })
            .HasFilter("\"IsEnded\" = false")
            .IsUnique();

        builder.Property(a => a.PositionId).IsRequired();
        builder.HasIndex(a => a.PositionId);

        builder.Property(a => a.DepartmentId);
        builder.HasIndex(a => a.DepartmentId);

        builder.Property(a => a.BusinessUnitId);
        builder.Property(a => a.CostCenterId);
        builder.Property(a => a.WorkLocationId);
        builder.Property(a => a.LegalEntityId);
        builder.Property(a => a.WorkArrangement).IsRequired();
        builder.Property(a => a.ReportingManagerEmploymentId);
        builder.Property(a => a.EffectiveStartDate).IsRequired();
        builder.Property(a => a.IsEnded).IsRequired();
        builder.Property(a => a.EndedDate);
        builder.Property(a => a.CreatedAtUtc).IsRequired();

        builder.OwnsMany(a => a.History, ConfigureHistory);
        builder.Navigation(a => a.History).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(a => a.ReportingHistory, ConfigureReportingHistory);
        builder.Navigation(a => a.ReportingHistory).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureHistory(
        OwnedNavigationBuilder<EmploymentAssignment, AssignmentHistoryRecord> history)
    {
        history.ToTable("employment_assignment_history_records");
        history.WithOwner().HasForeignKey("EmploymentAssignmentId");

        history.HasKey(h => h.Id);

        history.Property(h => h.Id)
            .HasConversion(
                new StronglyTypedIdValueConverter<AssignmentHistoryRecordId>(value => new AssignmentHistoryRecordId(value)))
            .ValueGeneratedNever();

        history.Property(h => h.PositionId).IsRequired();
        history.Property(h => h.DepartmentId);
        history.Property(h => h.BusinessUnitId);
        history.Property(h => h.CostCenterId);
        history.Property(h => h.WorkLocationId);
        history.Property(h => h.LegalEntityId);
        history.Property(h => h.EffectiveStartDate).IsRequired();
        history.Property(h => h.EffectiveEndDate).IsRequired();
        history.Property(h => h.CreatedAtUtc).IsRequired();

        history.HasIndex("EmploymentAssignmentId", nameof(AssignmentHistoryRecord.EffectiveStartDate));
    }

    private static void ConfigureReportingHistory(
        OwnedNavigationBuilder<EmploymentAssignment, ReportingAssignment> reporting)
    {
        reporting.ToTable("employment_reporting_assignments");
        reporting.WithOwner().HasForeignKey("EmploymentAssignmentId");

        reporting.HasKey(r => r.Id);

        reporting.Property(r => r.Id)
            .HasConversion(
                new StronglyTypedIdValueConverter<ReportingAssignmentId>(value => new ReportingAssignmentId(value)))
            .ValueGeneratedNever();

        reporting.Property(r => r.ReportingManagerEmploymentId).IsRequired();
        reporting.Property(r => r.EffectiveStartDate).IsRequired();
        reporting.Property(r => r.EffectiveEndDate);
        reporting.Property(r => r.CreatedAtUtc).IsRequired();
    }
}
