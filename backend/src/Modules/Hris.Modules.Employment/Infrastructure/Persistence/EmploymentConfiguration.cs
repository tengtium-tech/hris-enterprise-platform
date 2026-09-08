using Hris.Infrastructure.Persistence;
using Hris.Modules.Employment.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Employment.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="Domain.Employment"/>
/// Aggregate Root and its own owned child collections
/// (EmploymentStatusChange, ProbationRecord, CompensationRecord) plus its single
/// nullable owned <see cref="SeparationRecord"/>, per infrastructure/persistence.md's
/// own Table Mapping section -- the same <c>OwnsMany</c>/<c>OwnsOne</c>,
/// <c>PropertyAccessMode.Field</c> shape <c>OrganizationConfiguration</c> already
/// establishes, one level deep rather than five. Discovered automatically by
/// <c>HrisDbContext.OnModelCreating</c> via <c>PersistenceAssemblyRegistry</c>.
/// </summary>
public sealed class EmploymentConfiguration : IEntityTypeConfiguration<Domain.Employment>
{
    public void Configure(EntityTypeBuilder<Domain.Employment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("employments");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(new StronglyTypedIdValueConverter<EmploymentId>(value => new EmploymentId(value)))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.EmployeeId).IsRequired();
        builder.HasIndex(e => new { e.TenantId, e.EmployeeId });

        builder.Property(e => e.Number)
            .HasConversion(number => number.Value, value => EmploymentNumber.Create(value).Value)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.Number }).IsUnique();

        builder.Property(e => e.EmploymentType)
            .HasConversion(type => type.Value, value => Domain.EmploymentType.Create(value).Value)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Category)
            .HasConversion(category => category.Value, value => EmploymentCategory.Create(value).Value)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.LifecycleStage).IsRequired();
        builder.Property(e => e.OperationalStatus).IsRequired();
        builder.Property(e => e.IsPrimary).IsRequired();
        builder.Property(e => e.PriorEmploymentId);
        builder.Property(e => e.CreatedAtUtc).IsRequired();

        builder.OwnsOne(e => e.SeparationRecord, ConfigureSeparationRecord);
        builder.Navigation(e => e.SeparationRecord).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(e => e.StatusChanges, ConfigureStatusChange);
        builder.Navigation(e => e.StatusChanges).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(e => e.ProbationRecords, ConfigureProbationRecord);
        builder.Navigation(e => e.ProbationRecords).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(e => e.CompensationRecords, ConfigureCompensationRecord);
        builder.Navigation(e => e.CompensationRecords).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureSeparationRecord(OwnedNavigationBuilder<Domain.Employment, SeparationRecord> separation)
    {
        separation.ToTable("employment_separation_records");
        separation.WithOwner().HasForeignKey("EmploymentId");

        separation.HasKey(s => s.Id);

        separation.Property(s => s.Id)
            .HasConversion(new StronglyTypedIdValueConverter<SeparationRecordId>(value => new SeparationRecordId(value)))
            .ValueGeneratedNever();

        separation.Property(s => s.SeparationType).IsRequired();

        separation.Property(s => s.TerminationReason)
            .HasConversion(
                reason => reason == null ? null : reason.Value,
                value => value == null ? null : TerminationReason.Create(value).Value)
            .HasMaxLength(500);

        separation.Property(s => s.LastWorkingDate).IsRequired();
        separation.Property(s => s.EffectiveSeparationDate).IsRequired();
        separation.Property(s => s.CreatedAtUtc).IsRequired();
    }

    private static void ConfigureStatusChange(OwnedNavigationBuilder<Domain.Employment, EmploymentStatusChange> statusChange)
    {
        statusChange.ToTable("employment_status_changes");
        statusChange.WithOwner().HasForeignKey("EmploymentId");

        statusChange.HasKey(s => s.Id);

        statusChange.Property(s => s.Id)
            .HasConversion(
                new StronglyTypedIdValueConverter<EmploymentStatusChangeId>(value => new EmploymentStatusChangeId(value)))
            .ValueGeneratedNever();

        statusChange.Property(s => s.PreviousStatus).IsRequired();
        statusChange.Property(s => s.NewStatus).IsRequired();
        statusChange.Property(s => s.EffectiveDate).IsRequired();
        statusChange.Property(s => s.Reason).HasMaxLength(1000);
        statusChange.Property(s => s.CreatedAtUtc).IsRequired();

        statusChange.HasIndex("EmploymentId", nameof(EmploymentStatusChange.EffectiveDate));
    }

    private static void ConfigureProbationRecord(OwnedNavigationBuilder<Domain.Employment, ProbationRecord> probation)
    {
        probation.ToTable("employment_probation_records");
        probation.WithOwner().HasForeignKey("EmploymentId");

        probation.HasKey(p => p.Id);

        probation.Property(p => p.Id)
            .HasConversion(new StronglyTypedIdValueConverter<ProbationRecordId>(value => new ProbationRecordId(value)))
            .ValueGeneratedNever();

        probation.Property(p => p.StartDate).IsRequired();

        probation.Property(p => p.Duration)
            .HasConversion(duration => duration.Days, value => ProbationDuration.Create(value).Value)
            .IsRequired();

        probation.Property(p => p.ExpectedEvaluationDate).IsRequired();
        probation.Property(p => p.Outcome).IsRequired();
        probation.Property(p => p.ExtensionCount).IsRequired();
        probation.Property(p => p.CreatedAtUtc).IsRequired();
    }

    private static void ConfigureCompensationRecord(
        OwnedNavigationBuilder<Domain.Employment, CompensationRecord> compensation)
    {
        compensation.ToTable("employment_compensation_records");
        compensation.WithOwner().HasForeignKey("EmploymentId");

        compensation.HasKey(c => c.Id);

        compensation.Property(c => c.Id)
            .HasConversion(new StronglyTypedIdValueConverter<CompensationRecordId>(value => new CompensationRecordId(value)))
            .ValueGeneratedNever();

        compensation.OwnsOne(c => c.Amount, amount =>
        {
            amount.Property(a => a.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
            amount.Property(a => a.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
            amount.Property(a => a.Basis).HasColumnName("basis").IsRequired();
        });
        compensation.Navigation(c => c.Amount).UsePropertyAccessMode(PropertyAccessMode.Property).IsRequired();

        compensation.Property(c => c.EffectiveStartDate).IsRequired();
        compensation.Property(c => c.EffectiveEndDate);
        compensation.Property(c => c.ChangeSource).IsRequired();
        compensation.Property(c => c.ApprovalReference).HasMaxLength(200);
        compensation.Property(c => c.CreatedAtUtc).IsRequired();

        compensation.HasIndex("EmploymentId", nameof(CompensationRecord.EffectiveStartDate));
    }
}
