using FluentValidation;
using Hris.Modules.Position.Application.Commands;
using Hris.Modules.Position.Application.Queries;

namespace Hris.Modules.Position.Application.Validators;

/// <summary>
/// application-pipeline.md's Validation Behavior scope: "Required fields...
/// Business-independent validation." Deliberately does not re-check anything the
/// Domain layer's own factory/transition methods already enforce (number/title
/// shape, per-tenant uniqueness, lifecycle-state gating, reporting-hierarchy rules)
/// -- the identical separation <c>OrganizationCommandValidators</c> already states
/// for its own set.
/// </summary>
public sealed class CreatePositionCommandValidator : AbstractValidator<CreatePositionCommand>
{
    public CreatePositionCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Number).NotEmpty();
        RuleFor(c => c.Title).NotEmpty();
        RuleFor(c => c.PositionType).NotEmpty();
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.JobFamilyId).NotEmpty();
        RuleFor(c => c.JobClassificationId).NotEmpty();
        RuleFor(c => c.JobGradeId).NotEmpty();
        RuleFor(c => c.AuthorizedHeadcount).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdatePositionCommandValidator : AbstractValidator<UpdatePositionCommand>
{
    public UpdatePositionCommandValidator()
    {
        RuleFor(c => c.PositionId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.JobFamilyId).NotEmpty();
        RuleFor(c => c.JobClassificationId).NotEmpty();
        RuleFor(c => c.JobGradeId).NotEmpty();
    }
}

public sealed class ActivatePositionCommandValidator : AbstractValidator<ActivatePositionCommand>
{
    public ActivatePositionCommandValidator()
    {
        RuleFor(c => c.PositionId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class DeactivatePositionCommandValidator : AbstractValidator<DeactivatePositionCommand>
{
    public DeactivatePositionCommandValidator()
    {
        RuleFor(c => c.PositionId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class ArchivePositionCommandValidator : AbstractValidator<ArchivePositionCommand>
{
    public ArchivePositionCommandValidator()
    {
        RuleFor(c => c.PositionId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class AssignReportingPositionCommandValidator : AbstractValidator<AssignReportingPositionCommand>
{
    public AssignReportingPositionCommandValidator()
    {
        RuleFor(c => c.PositionId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.ReportingPositionId).NotEmpty();
    }
}

public sealed class RemoveReportingPositionCommandValidator : AbstractValidator<RemoveReportingPositionCommand>
{
    public RemoveReportingPositionCommandValidator()
    {
        RuleFor(c => c.PositionId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class UpdateAuthorizedHeadcountCommandValidator : AbstractValidator<UpdateAuthorizedHeadcountCommand>
{
    public UpdateAuthorizedHeadcountCommandValidator()
    {
        RuleFor(c => c.PositionId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.AuthorizedHeadcount).GreaterThanOrEqualTo(0);
    }
}

public sealed class MarkPositionVacantCommandValidator : AbstractValidator<MarkPositionVacantCommand>
{
    public MarkPositionVacantCommandValidator()
    {
        RuleFor(c => c.PositionId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class MarkPositionFilledCommandValidator : AbstractValidator<MarkPositionFilledCommand>
{
    public MarkPositionFilledCommandValidator()
    {
        RuleFor(c => c.PositionId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class CreateJobFamilyCommandValidator : AbstractValidator<CreateJobFamilyCommand>
{
    public CreateJobFamilyCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Code).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
    }
}

public sealed class UpdateJobFamilyCommandValidator : AbstractValidator<UpdateJobFamilyCommand>
{
    public UpdateJobFamilyCommandValidator()
    {
        RuleFor(c => c.JobFamilyId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class ActivateJobFamilyCommandValidator : AbstractValidator<ActivateJobFamilyCommand>
{
    public ActivateJobFamilyCommandValidator()
    {
        RuleFor(c => c.JobFamilyId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class DeactivateJobFamilyCommandValidator : AbstractValidator<DeactivateJobFamilyCommand>
{
    public DeactivateJobFamilyCommandValidator()
    {
        RuleFor(c => c.JobFamilyId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class ArchiveJobFamilyCommandValidator : AbstractValidator<ArchiveJobFamilyCommand>
{
    public ArchiveJobFamilyCommandValidator()
    {
        RuleFor(c => c.JobFamilyId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class CreateJobClassificationCommandValidator : AbstractValidator<CreateJobClassificationCommand>
{
    public CreateJobClassificationCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Code).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
    }
}

public sealed class UpdateJobClassificationCommandValidator : AbstractValidator<UpdateJobClassificationCommand>
{
    public UpdateJobClassificationCommandValidator()
    {
        RuleFor(c => c.JobClassificationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class ActivateJobClassificationCommandValidator : AbstractValidator<ActivateJobClassificationCommand>
{
    public ActivateJobClassificationCommandValidator()
    {
        RuleFor(c => c.JobClassificationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class DeactivateJobClassificationCommandValidator : AbstractValidator<DeactivateJobClassificationCommand>
{
    public DeactivateJobClassificationCommandValidator()
    {
        RuleFor(c => c.JobClassificationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class ArchiveJobClassificationCommandValidator : AbstractValidator<ArchiveJobClassificationCommand>
{
    public ArchiveJobClassificationCommandValidator()
    {
        RuleFor(c => c.JobClassificationId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class CreateJobGradeCommandValidator : AbstractValidator<CreateJobGradeCommand>
{
    public CreateJobGradeCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Code).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
    }
}

public sealed class UpdateJobGradeCommandValidator : AbstractValidator<UpdateJobGradeCommand>
{
    public UpdateJobGradeCommandValidator()
    {
        RuleFor(c => c.JobGradeId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class ActivateJobGradeCommandValidator : AbstractValidator<ActivateJobGradeCommand>
{
    public ActivateJobGradeCommandValidator()
    {
        RuleFor(c => c.JobGradeId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class DeactivateJobGradeCommandValidator : AbstractValidator<DeactivateJobGradeCommand>
{
    public DeactivateJobGradeCommandValidator()
    {
        RuleFor(c => c.JobGradeId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class ArchiveJobGradeCommandValidator : AbstractValidator<ArchiveJobGradeCommand>
{
    public ArchiveJobGradeCommandValidator()
    {
        RuleFor(c => c.JobGradeId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}
