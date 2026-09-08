using Hris.Infrastructure.Persistence;
using Hris.Modules.Position.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Position.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="Domain.Position"/> Aggregate
/// Root. Unlike Organization's own five-level nested owned hierarchy, this Aggregate
/// has no owned navigations at all -- every organizational, workforce-classification,
/// and reporting reference is a plain scalar <see cref="Guid"/> column, per this
/// module's own standing "reference by identifier" rule -- so this configuration is
/// a single flat table with several single-column Value Object conversions, the same
/// shape <c>WorkLocationConfiguration</c> already uses for
/// <c>WorkLocationTimeZone</c>.
///
/// <see cref="Domain.Position.ReportingPositionId"/> is indexed (not a foreign key
/// constraint, since EF Core has no dedicated Aggregate Root type to declare one
/// against here without introducing a self-referencing navigation this Domain layer
/// deliberately does not expose) so <c>PositionRepository.WouldCreateCircularReportingAsync</c>'s
/// own ancestor-chain walk can run efficiently.
/// </summary>
public sealed class PositionConfiguration : IEntityTypeConfiguration<Domain.Position>
{
    public void Configure(EntityTypeBuilder<Domain.Position> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasConversion(new StronglyTypedIdValueConverter<PositionId>(value => new PositionId(value)))
            .ValueGeneratedNever();

        builder.Property(p => p.TenantId).IsRequired();
        builder.HasIndex(p => p.TenantId);

        builder.Property(p => p.Number)
            .HasConversion(number => number.Value, value => PositionNumber.Create(value).Value)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(p => new { p.TenantId, p.Number }).IsUnique();

        builder.Property(p => p.Title)
            .HasConversion(title => title.Value, value => PositionTitle.Create(value).Value)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.PositionType)
            .HasConversion(type => type.Value, value => PositionType.Create(value).Value)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.OrganizationId).IsRequired();
        builder.HasIndex(p => p.OrganizationId);

        builder.Property(p => p.LegalEntityId);
        builder.Property(p => p.BusinessUnitId);
        builder.Property(p => p.DivisionId);
        builder.Property(p => p.DepartmentId);
        builder.Property(p => p.SectionId);
        builder.Property(p => p.TeamId);
        builder.Property(p => p.WorkLocationId);
        builder.Property(p => p.CostCenterId);

        builder.Property(p => p.JobFamilyId).IsRequired();
        builder.Property(p => p.JobClassificationId).IsRequired();
        builder.Property(p => p.JobGradeId).IsRequired();

        builder.Property(p => p.ReportingPositionId);
        builder.HasIndex(p => p.ReportingPositionId);

        builder.Property(p => p.AuthorizedHeadcount)
            .HasConversion(headcount => headcount.Value, value => AuthorizedHeadcount.Create(value).Value)
            .IsRequired();

        builder.Property(p => p.Status).IsRequired();
        builder.Property(p => p.VacancyStatus).IsRequired();
        builder.Property(p => p.CreatedAtUtc).IsRequired();
    }
}
