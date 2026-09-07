using Hris.Infrastructure.Persistence;
using Hris.Modules.Organization.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Organization.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="Domain.Organization"/>
/// Aggregate Root and its own five-level nested owned hierarchy
/// (BusinessUnit -&gt; Division -&gt; Department -&gt; Section -&gt; Team, plus
/// CostCenter as a direct sibling of BusinessUnit), per
/// infrastructure/persistence.md's own Table Mapping section and
/// coding-standards.md's Infrastructure Layer convention -- the same
/// <c>OwnsMany</c>-chained-within-<c>OwnsMany</c>, <c>PropertyAccessMode.Field</c>
/// shape <c>ConnectorConfiguration</c> establishes for a single level, extended here
/// across every level this Aggregate's own child hierarchy needs.
///
/// <see cref="Division.BusinessUnitId"/>, <see cref="Department.DivisionId"/>,
/// <see cref="Section.DepartmentId"/>, and <see cref="Team.SectionId"/> are each a
/// real, explicit CLR property on their own type (needed for
/// <see cref="Domain.Organization.MoveDivision"/>/<see cref="Domain.Organization.MoveDepartment"/>
/// to reassign it), so each one's own <c>HasForeignKey</c> call below points at that
/// real property directly rather than declaring a second, redundant shadow property
/// with the same name. <see cref="BusinessUnit"/> and <see cref="CostCenter"/> have
/// no such property of their own (nothing ever reassigns which Organization a
/// BusinessUnit or CostCenter belongs to), so their own owner relationship uses a
/// genuine shadow property instead, the identical choice
/// <c>ConnectorConfiguration</c> already makes for <c>DataMapping</c>'s own
/// "ConnectorId".
///
/// DEPT-002 (Department code, tenant-wide) and CC-001 (Cost Center code,
/// tenant-wide) have no database-level unique index here, unlike ORG-001/ORG-002
/// below: both nested entity types sit several owned-table levels beneath
/// Organization's own TenantId column, and denormalizing TenantId onto every
/// intermediate table purely to support one constraint would be a real schema cost
/// for a rule the Application layer's own repository check
/// (<see cref="IOrganizationRepository.ExistsDepartmentWithCodeAsync"/>/
/// <see cref="IOrganizationRepository.ExistsCostCenterWithCodeAsync"/>) already
/// enforces as an expected business outcome, per result-pattern.md. This is a
/// deliberately scoped gap, not an oversight: a concurrent double-insert racing past
/// both checks is the one failure mode it does not close.
///
/// Discovered automatically by <c>HrisDbContext.OnModelCreating</c> via
/// <c>PersistenceAssemblyRegistry</c>.
/// </summary>
public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Domain.Organization>
{
    public void Configure(EntityTypeBuilder<Domain.Organization> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasConversion(new StronglyTypedIdValueConverter<OrganizationId>(value => new OrganizationId(value)))
            .ValueGeneratedNever();

        builder.Property(o => o.TenantId).IsRequired();
        builder.HasIndex(o => o.TenantId);

        builder.Property(o => o.Name)
            .HasConversion(name => name.Value, value => OrganizationName.Create(value).Value)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(o => o.Code)
            .HasConversion(code => code.Value, value => OrganizationCode.Create(value).Value)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(o => new { o.TenantId, o.Code }).IsUnique();
        builder.HasIndex(o => new { o.TenantId, o.Name }).IsUnique();

        builder.Property(o => o.LegalEntityId);
        builder.Property(o => o.Description).HasMaxLength(2000);
        builder.Property(o => o.Status).IsRequired();
        builder.Property(o => o.CreatedAtUtc).IsRequired();

        builder.OwnsMany(o => o.BusinessUnits, ConfigureBusinessUnit);
        builder.Navigation(o => o.BusinessUnits).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(o => o.CostCenters, ConfigureCostCenter);
        builder.Navigation(o => o.CostCenters).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureBusinessUnit(OwnedNavigationBuilder<Domain.Organization, BusinessUnit> businessUnit)
    {
        businessUnit.ToTable("organization_business_units");
        businessUnit.WithOwner().HasForeignKey("OrganizationId");

        businessUnit.HasKey(bu => bu.Id);

        businessUnit.Property(bu => bu.Id)
            .HasConversion(new StronglyTypedIdValueConverter<BusinessUnitId>(value => new BusinessUnitId(value)))
            .ValueGeneratedNever();

        businessUnit.Property(bu => bu.Name)
            .HasConversion(name => name.Value, value => BusinessUnitName.Create(value).Value)
            .HasMaxLength(200)
            .IsRequired();

        businessUnit.Property(bu => bu.Status).IsRequired();
        businessUnit.Property(bu => bu.CreatedAtUtc).IsRequired();

        businessUnit.OwnsMany(bu => bu.Divisions, ConfigureDivision);
        businessUnit.Navigation(bu => bu.Divisions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureDivision(OwnedNavigationBuilder<BusinessUnit, Division> division)
    {
        division.ToTable("organization_divisions");
        division.WithOwner().HasForeignKey(d => d.BusinessUnitId);

        division.HasKey(d => d.Id);

        division.Property(d => d.Id)
            .HasConversion(new StronglyTypedIdValueConverter<DivisionId>(value => new DivisionId(value)))
            .ValueGeneratedNever();

        division.Property(d => d.BusinessUnitId)
            .HasConversion(new StronglyTypedIdValueConverter<BusinessUnitId>(value => new BusinessUnitId(value)));

        division.Property(d => d.Name)
            .HasConversion(name => name.Value, value => DivisionName.Create(value).Value)
            .HasMaxLength(200)
            .IsRequired();

        division.Property(d => d.Status).IsRequired();
        division.Property(d => d.CreatedAtUtc).IsRequired();

        division.OwnsMany(d => d.Departments, ConfigureDepartment);
        division.Navigation(d => d.Departments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureDepartment(OwnedNavigationBuilder<Division, Department> department)
    {
        department.ToTable("organization_departments");
        department.WithOwner().HasForeignKey(dep => dep.DivisionId);

        department.HasKey(dep => dep.Id);

        department.Property(dep => dep.Id)
            .HasConversion(new StronglyTypedIdValueConverter<DepartmentId>(value => new DepartmentId(value)))
            .ValueGeneratedNever();

        department.Property(dep => dep.DivisionId)
            .HasConversion(new StronglyTypedIdValueConverter<DivisionId>(value => new DivisionId(value)));

        department.Property(dep => dep.Name)
            .HasConversion(name => name.Value, value => DepartmentName.Create(value).Value)
            .HasMaxLength(200)
            .IsRequired();

        department.Property(dep => dep.Code)
            .HasConversion(code => code.Value, value => DepartmentCode.Create(value).Value)
            .HasMaxLength(20)
            .IsRequired();

        department.Property(dep => dep.Status).IsRequired();

        department.Property(dep => dep.MergedIntoDepartmentId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new DepartmentId(value.Value) : (DepartmentId?)null);

        department.Property(dep => dep.SplitFromDepartmentId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new DepartmentId(value.Value) : (DepartmentId?)null);

        department.Property(dep => dep.CreatedAtUtc).IsRequired();

        department.OwnsMany(dep => dep.Sections, ConfigureSection);
        department.Navigation(dep => dep.Sections).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureSection(OwnedNavigationBuilder<Department, Section> section)
    {
        section.ToTable("organization_sections");
        section.WithOwner().HasForeignKey(s => s.DepartmentId);

        section.HasKey(s => s.Id);

        section.Property(s => s.Id)
            .HasConversion(new StronglyTypedIdValueConverter<SectionId>(value => new SectionId(value)))
            .ValueGeneratedNever();

        section.Property(s => s.DepartmentId)
            .HasConversion(new StronglyTypedIdValueConverter<DepartmentId>(value => new DepartmentId(value)));

        section.Property(s => s.Name)
            .HasConversion(name => name.Value, value => SectionName.Create(value).Value)
            .HasMaxLength(200)
            .IsRequired();

        section.Property(s => s.Status).IsRequired();
        section.Property(s => s.CreatedAtUtc).IsRequired();

        section.OwnsMany(s => s.Teams, ConfigureTeam);
        section.Navigation(s => s.Teams).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureTeam(OwnedNavigationBuilder<Section, Team> team)
    {
        team.ToTable("organization_teams");
        team.WithOwner().HasForeignKey(t => t.SectionId);

        team.HasKey(t => t.Id);

        team.Property(t => t.Id)
            .HasConversion(new StronglyTypedIdValueConverter<TeamId>(value => new TeamId(value)))
            .ValueGeneratedNever();

        team.Property(t => t.SectionId)
            .HasConversion(new StronglyTypedIdValueConverter<SectionId>(value => new SectionId(value)));

        team.Property(t => t.Name)
            .HasConversion(name => name.Value, value => TeamName.Create(value).Value)
            .HasMaxLength(200)
            .IsRequired();

        team.Property(t => t.Status).IsRequired();
        team.Property(t => t.CreatedAtUtc).IsRequired();
    }

    private static void ConfigureCostCenter(OwnedNavigationBuilder<Domain.Organization, CostCenter> costCenter)
    {
        costCenter.ToTable("organization_cost_centers");
        costCenter.WithOwner().HasForeignKey("OrganizationId");

        costCenter.HasKey(cc => cc.Id);

        costCenter.Property(cc => cc.Id)
            .HasConversion(new StronglyTypedIdValueConverter<CostCenterId>(value => new CostCenterId(value)))
            .ValueGeneratedNever();

        costCenter.Property(cc => cc.Code)
            .HasConversion(code => code.Value, value => CostCenterCode.Create(value).Value)
            .HasMaxLength(20)
            .IsRequired();

        costCenter.Property(cc => cc.Description).HasMaxLength(2000);
        costCenter.Property(cc => cc.Status).IsRequired();
        costCenter.Property(cc => cc.CreatedAtUtc).IsRequired();
    }
}
