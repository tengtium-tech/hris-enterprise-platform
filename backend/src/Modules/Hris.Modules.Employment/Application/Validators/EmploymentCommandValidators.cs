using FluentValidation;
using Hris.Modules.Employment.Application.Commands;

namespace Hris.Modules.Employment.Application.Validators;

/// <summary>
/// application-pipeline.md's Validation Behavior scope: "Required fields...
/// Business-independent validation." Deliberately does not re-check anything the
/// Domain layer's own factory/transition methods already enforce (number/type/
/// category shape, per-tenant uniqueness, lifecycle-state gating, probation and
/// separation rules) -- the identical separation <c>PositionCommandValidators</c>
/// and <c>OrganizationCommandValidators</c> already state for their own sets.
/// </summary>
public sealed class CreateEmploymentCommandValidator : AbstractValidator<CreateEmploymentCommand>
{
    public CreateEmploymentCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.Number).NotEmpty();
        RuleFor(c => c.EmploymentType).NotEmpty();
        RuleFor(c => c.Category).NotEmpty();
        RuleFor(c => c.PrimaryEmploymentId).NotEmpty().When(c => !c.IsPrimary);
    }
}

public sealed class ActivateEmploymentCommandValidator : AbstractValidator<ActivateEmploymentCommand>
{
    public ActivateEmploymentCommandValidator()
    {
        RuleFor(c => c.EmploymentId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class ChangeEmploymentTypeCommandValidator : AbstractValidator<ChangeEmploymentTypeCommand>
{
    public ChangeEmploymentTypeCommandValidator()
    {
        RuleFor(c => c.EmploymentId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.NewType).NotEmpty();
    }
}

public sealed class ChangeEmploymentCategoryCommandValidator : AbstractValidator<ChangeEmploymentCategoryCommand>
{
    public ChangeEmploymentCategoryCommandValidator()
    {
        RuleFor(c => c.EmploymentId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.NewCategory).NotEmpty();
    }
}

public sealed class ChangePrimaryEmploymentCommandValidator : AbstractValidator<ChangePrimaryEmploymentCommand>
{
    public ChangePrimaryEmploymentCommandValidator()
    {
        RuleFor(c => c.EmploymentId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class SuspendEmploymentCommandValidator : AbstractValidator<SuspendEmploymentCommand>
{
    public SuspendEmploymentCommandValidator()
    {
        RuleFor(c => c.EmploymentId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class ReinstateEmploymentCommandValidator : AbstractValidator<ReinstateEmploymentCommand>
{
    public ReinstateEmploymentCommandValidator()
    {
        RuleFor(c => c.EmploymentId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class SecondEmploymentCommandValidator : AbstractValidator<SecondEmploymentCommand>
{
    public SecondEmploymentCommandValidator()
    {
        RuleFor(c => c.EmploymentId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class SeparateEmploymentCommandValidator : AbstractValidator<SeparateEmploymentCommand>
{
    public SeparateEmploymentCommandValidator()
    {
        RuleFor(c => c.EmploymentId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.LastWorkingDate).LessThanOrEqualTo(c => c.EffectiveSeparationDate);
    }
}

public sealed class StartProbationCommandValidator : AbstractValidator<StartProbationCommand>
{
    public StartProbationCommandValidator()
    {
        RuleFor(c => c.EmploymentId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.DurationDays).GreaterThan(0);
    }
}

public sealed class ExtendProbationCommandValidator : AbstractValidator<ExtendProbationCommand>
{
    public ExtendProbationCommandValidator()
    {
        RuleFor(c => c.EmploymentId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.AdditionalDurationDays).GreaterThan(0);
    }
}

public sealed class ConfirmEmploymentCommandValidator : AbstractValidator<ConfirmEmploymentCommand>
{
    public ConfirmEmploymentCommandValidator()
    {
        RuleFor(c => c.EmploymentId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class FailProbationCommandValidator : AbstractValidator<FailProbationCommand>
{
    public FailProbationCommandValidator()
    {
        RuleFor(c => c.EmploymentId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.LastWorkingDate).LessThanOrEqualTo(c => c.EffectiveSeparationDate);
    }
}

public sealed class RecordEmploymentCompensationCommandValidator : AbstractValidator<RecordEmploymentCompensationCommand>
{
    public RecordEmploymentCompensationCommandValidator()
    {
        RuleFor(c => c.EmploymentId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Amount).GreaterThanOrEqualTo(0);
        RuleFor(c => c.CurrencyCode).NotEmpty();
    }
}

public sealed class CreateEmploymentContractCommandValidator : AbstractValidator<CreateEmploymentContractCommand>
{
    public CreateEmploymentContractCommandValidator()
    {
        RuleFor(c => c.EmploymentId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.ContractType).NotEmpty();
    }
}

public sealed class ApproveEmploymentContractCommandValidator : AbstractValidator<ApproveEmploymentContractCommand>
{
    public ApproveEmploymentContractCommandValidator()
    {
        RuleFor(c => c.EmploymentContractId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class ActivateEmploymentContractCommandValidator : AbstractValidator<ActivateEmploymentContractCommand>
{
    public ActivateEmploymentContractCommandValidator()
    {
        RuleFor(c => c.EmploymentContractId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class RenewEmploymentContractCommandValidator : AbstractValidator<RenewEmploymentContractCommand>
{
    public RenewEmploymentContractCommandValidator()
    {
        RuleFor(c => c.EmploymentContractId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class ExtendEmploymentContractCommandValidator : AbstractValidator<ExtendEmploymentContractCommand>
{
    public ExtendEmploymentContractCommandValidator()
    {
        RuleFor(c => c.EmploymentContractId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class SupersedeEmploymentContractCommandValidator : AbstractValidator<SupersedeEmploymentContractCommand>
{
    public SupersedeEmploymentContractCommandValidator()
    {
        RuleFor(c => c.EmploymentContractId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class CloseEmploymentContractCommandValidator : AbstractValidator<CloseEmploymentContractCommand>
{
    public CloseEmploymentContractCommandValidator()
    {
        RuleFor(c => c.EmploymentContractId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class CancelEmploymentContractCommandValidator : AbstractValidator<CancelEmploymentContractCommand>
{
    public CancelEmploymentContractCommandValidator()
    {
        RuleFor(c => c.EmploymentContractId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class AssignPositionCommandValidator : AbstractValidator<AssignPositionCommand>
{
    public AssignPositionCommandValidator()
    {
        RuleFor(c => c.EmploymentId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.PositionId).NotEmpty();
    }
}

public sealed class TransferEmploymentCommandValidator : AbstractValidator<TransferEmploymentCommand>
{
    public TransferEmploymentCommandValidator()
    {
        RuleFor(c => c.EmploymentAssignmentId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.NewPositionId).NotEmpty();
    }
}

public sealed class PromoteEmploymentCommandValidator : AbstractValidator<PromoteEmploymentCommand>
{
    public PromoteEmploymentCommandValidator()
    {
        RuleFor(c => c.EmploymentAssignmentId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.NewPositionId).NotEmpty();
    }
}

public sealed class DemoteEmploymentCommandValidator : AbstractValidator<DemoteEmploymentCommand>
{
    public DemoteEmploymentCommandValidator()
    {
        RuleFor(c => c.EmploymentAssignmentId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.NewPositionId).NotEmpty();
    }
}

public sealed class ChangeReportingManagerCommandValidator : AbstractValidator<ChangeReportingManagerCommand>
{
    public ChangeReportingManagerCommandValidator()
    {
        RuleFor(c => c.EmploymentAssignmentId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.NewReportingManagerEmploymentId).NotEmpty();
    }
}

public sealed class EndAssignmentCommandValidator : AbstractValidator<EndAssignmentCommand>
{
    public EndAssignmentCommandValidator()
    {
        RuleFor(c => c.EmploymentAssignmentId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}
