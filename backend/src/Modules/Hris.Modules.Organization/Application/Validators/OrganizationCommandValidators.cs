using FluentValidation;
using Hris.Modules.Organization.Application.Commands;
using Hris.Modules.Organization.Application.Queries;

namespace Hris.Modules.Organization.Application.Validators;

/// <summary>
/// application-pipeline.md's Validation Behavior scope: "Required fields...
/// Business-independent validation." Deliberately does not re-check anything the
/// Domain layer's own factory/transition methods already enforce (name/code shape,
/// per-parent uniqueness, lifecycle-state gating) -- the identical separation every
/// other framework's own validators file states for its own set. Every command here
/// also gets its identical Domain-level check (a required string that arrives empty
/// still fails with the specific <c>OrganizationErrors</c> entry, not a generic
/// FluentValidation message) -- this file exists to fail fast, before a repository
/// round trip, not to be the only line of defense.
/// </summary>
public sealed class CreateOrganizationCommandValidator : AbstractValidator<CreateOrganizationCommand>
{
    public CreateOrganizationCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.Code).NotEmpty();
    }
}

public sealed class RenameOrganizationCommandValidator : AbstractValidator<RenameOrganizationCommand>
{
    public RenameOrganizationCommandValidator()
    {
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.NewName).NotEmpty();
    }
}

public sealed class UpdateOrganizationCommandValidator : AbstractValidator<UpdateOrganizationCommand>
{
    public UpdateOrganizationCommandValidator()
    {
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class ArchiveOrganizationCommandValidator : AbstractValidator<ArchiveOrganizationCommand>
{
    public ArchiveOrganizationCommandValidator()
    {
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class RestoreOrganizationCommandValidator : AbstractValidator<RestoreOrganizationCommand>
{
    public RestoreOrganizationCommandValidator()
    {
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class CreateBusinessUnitCommandValidator : AbstractValidator<CreateBusinessUnitCommand>
{
    public CreateBusinessUnitCommandValidator()
    {
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
    }
}

public sealed class RenameBusinessUnitCommandValidator : AbstractValidator<RenameBusinessUnitCommand>
{
    public RenameBusinessUnitCommandValidator()
    {
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.BusinessUnitId).NotEmpty();
        RuleFor(c => c.NewName).NotEmpty();
    }
}

public sealed class ArchiveBusinessUnitCommandValidator : AbstractValidator<ArchiveBusinessUnitCommand>
{
    public ArchiveBusinessUnitCommandValidator()
    {
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.BusinessUnitId).NotEmpty();
    }
}

public sealed class RestoreBusinessUnitCommandValidator : AbstractValidator<RestoreBusinessUnitCommand>
{
    public RestoreBusinessUnitCommandValidator()
    {
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.BusinessUnitId).NotEmpty();
    }
}

public sealed class CreateDivisionCommandValidator : AbstractValidator<CreateDivisionCommand>
{
    public CreateDivisionCommandValidator()
    {
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.BusinessUnitId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
    }
}

public sealed class RenameDivisionCommandValidator : AbstractValidator<RenameDivisionCommand>
{
    public RenameDivisionCommandValidator()
    {
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.DivisionId).NotEmpty();
        RuleFor(c => c.NewName).NotEmpty();
    }
}

public sealed class MoveDivisionCommandValidator : AbstractValidator<MoveDivisionCommand>
{
    public MoveDivisionCommandValidator()
    {
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.DivisionId).NotEmpty();
        RuleFor(c => c.NewBusinessUnitId).NotEmpty();
    }
}

public sealed class ArchiveDivisionCommandValidator : AbstractValidator<ArchiveDivisionCommand>
{
    public ArchiveDivisionCommandValidator()
    {
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.DivisionId).NotEmpty();
    }
}

public sealed class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator()
    {
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.DivisionId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.Code).NotEmpty();
    }
}

public sealed class RenameDepartmentCommandValidator : AbstractValidator<RenameDepartmentCommand>
{
    public RenameDepartmentCommandValidator()
    {
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.DepartmentId).NotEmpty();
        RuleFor(c => c.NewName).NotEmpty();
    }
}

public sealed class MoveDepartmentCommandValidator : AbstractValidator<MoveDepartmentCommand>
{
    public MoveDepartmentCommandValidator()
    {
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.DepartmentId).NotEmpty();
        RuleFor(c => c.NewDivisionId).NotEmpty();
    }
}

public sealed class MergeDepartmentsCommandValidator : AbstractValidator<MergeDepartmentsCommand>
{
    public MergeDepartmentsCommandValidator()
    {
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.SourceDepartmentIds).NotEmpty();
        RuleFor(c => c.SurvivingName).NotEmpty();
        RuleFor(c => c.SurvivingCode).NotEmpty();
    }
}

public sealed class SplitDepartmentCommandValidator : AbstractValidator<SplitDepartmentCommand>
{
    public SplitDepartmentCommandValidator()
    {
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.SourceDepartmentId).NotEmpty();
        RuleFor(c => c.NewDepartments).NotEmpty();
    }
}

public sealed class ArchiveDepartmentCommandValidator : AbstractValidator<ArchiveDepartmentCommand>
{
    public ArchiveDepartmentCommandValidator()
    {
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.DepartmentId).NotEmpty();
    }
}

public sealed class RestoreDepartmentCommandValidator : AbstractValidator<RestoreDepartmentCommand>
{
    public RestoreDepartmentCommandValidator()
    {
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.DepartmentId).NotEmpty();
    }
}

public sealed class CreateSectionCommandValidator : AbstractValidator<CreateSectionCommand>
{
    public CreateSectionCommandValidator()
    {
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.DepartmentId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
    }
}

public sealed class RenameSectionCommandValidator : AbstractValidator<RenameSectionCommand>
{
    public RenameSectionCommandValidator()
    {
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.SectionId).NotEmpty();
        RuleFor(c => c.NewName).NotEmpty();
    }
}

public sealed class ArchiveSectionCommandValidator : AbstractValidator<ArchiveSectionCommand>
{
    public ArchiveSectionCommandValidator()
    {
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.SectionId).NotEmpty();
    }
}

public sealed class CreateTeamCommandValidator : AbstractValidator<CreateTeamCommand>
{
    public CreateTeamCommandValidator()
    {
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.SectionId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
    }
}

public sealed class RenameTeamCommandValidator : AbstractValidator<RenameTeamCommand>
{
    public RenameTeamCommandValidator()
    {
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.TeamId).NotEmpty();
        RuleFor(c => c.NewName).NotEmpty();
    }
}

public sealed class ArchiveTeamCommandValidator : AbstractValidator<ArchiveTeamCommand>
{
    public ArchiveTeamCommandValidator()
    {
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.TeamId).NotEmpty();
    }
}

public sealed class CreateCostCenterCommandValidator : AbstractValidator<CreateCostCenterCommand>
{
    public CreateCostCenterCommandValidator()
    {
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Code).NotEmpty();
    }
}

public sealed class UpdateCostCenterCommandValidator : AbstractValidator<UpdateCostCenterCommand>
{
    public UpdateCostCenterCommandValidator()
    {
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.CostCenterId).NotEmpty();
    }
}

public sealed class ArchiveCostCenterCommandValidator : AbstractValidator<ArchiveCostCenterCommand>
{
    public ArchiveCostCenterCommandValidator()
    {
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.CostCenterId).NotEmpty();
    }
}

public sealed class CreateWorkLocationCommandValidator : AbstractValidator<CreateWorkLocationCommand>
{
    public CreateWorkLocationCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Code).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.AddressLine1).NotEmpty();
        RuleFor(c => c.City).NotEmpty();
        RuleFor(c => c.Country).NotEmpty();
        RuleFor(c => c.TimeZone).NotEmpty();
    }
}

public sealed class UpdateWorkLocationCommandValidator : AbstractValidator<UpdateWorkLocationCommand>
{
    public UpdateWorkLocationCommandValidator()
    {
        RuleFor(c => c.WorkLocationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.AddressLine1).NotEmpty();
        RuleFor(c => c.City).NotEmpty();
        RuleFor(c => c.Country).NotEmpty();
        RuleFor(c => c.TimeZone).NotEmpty();
    }
}

public sealed class ArchiveWorkLocationCommandValidator : AbstractValidator<ArchiveWorkLocationCommand>
{
    public ArchiveWorkLocationCommandValidator()
    {
        RuleFor(c => c.WorkLocationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class RestoreWorkLocationCommandValidator : AbstractValidator<RestoreWorkLocationCommand>
{
    public RestoreWorkLocationCommandValidator()
    {
        RuleFor(c => c.WorkLocationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class CreateLegalEntityCommandValidator : AbstractValidator<CreateLegalEntityCommand>
{
    public CreateLegalEntityCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Code).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.BusinessRegistrationNumber).NotEmpty();
        RuleFor(c => c.Country).NotEmpty();
    }
}

public sealed class UpdateLegalEntityCommandValidator : AbstractValidator<UpdateLegalEntityCommand>
{
    public UpdateLegalEntityCommandValidator()
    {
        RuleFor(c => c.LegalEntityId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.Country).NotEmpty();
    }
}

public sealed class ArchiveLegalEntityCommandValidator : AbstractValidator<ArchiveLegalEntityCommand>
{
    public ArchiveLegalEntityCommandValidator()
    {
        RuleFor(c => c.LegalEntityId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class GetOrganizationQueryValidator : AbstractValidator<GetOrganizationQuery>
{
    public GetOrganizationQueryValidator()
    {
        RuleFor(q => q.OrganizationId).NotEmpty();
        RuleFor(q => q.TenantId).NotEmpty();
    }
}

public sealed class ListOrganizationsQueryValidator : AbstractValidator<ListOrganizationsQuery>
{
    public ListOrganizationsQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
    }
}

public sealed class GetWorkLocationQueryValidator : AbstractValidator<GetWorkLocationQuery>
{
    public GetWorkLocationQueryValidator()
    {
        RuleFor(q => q.WorkLocationId).NotEmpty();
        RuleFor(q => q.TenantId).NotEmpty();
    }
}

public sealed class ListWorkLocationsQueryValidator : AbstractValidator<ListWorkLocationsQuery>
{
    public ListWorkLocationsQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
    }
}

public sealed class GetLegalEntityQueryValidator : AbstractValidator<GetLegalEntityQuery>
{
    public GetLegalEntityQueryValidator()
    {
        RuleFor(q => q.LegalEntityId).NotEmpty();
        RuleFor(q => q.TenantId).NotEmpty();
    }
}

public sealed class ListLegalEntitiesQueryValidator : AbstractValidator<ListLegalEntitiesQuery>
{
    public ListLegalEntitiesQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
    }
}
