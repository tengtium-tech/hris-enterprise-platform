using FluentValidation;
using Hris.Modules.Leave.Application.Commands;

namespace Hris.Modules.Leave.Application.Validators;

/// <summary>
/// Shape-and-existence validation for <c>LeavePolicy</c> commands. Versioning and
/// assignment-overlap rules (LV-010, LV-012) belong to the aggregate; the statutory floor
/// (LV-013) belongs to <c>PublishLeavePolicyCommandHandler</c>, since it needs
/// <c>LeaveType</c> data no validator resolves. Here we confirm the request carries the
/// identifiers and configuration it must (application/validations.md).
/// </summary>
public sealed class DefineLeavePolicyCommandValidator : AbstractValidator<DefineLeavePolicyCommand>
{
    public DefineLeavePolicyCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.LeaveTypeId).NotEmpty();
        RuleFor(c => c.Ruleset).NotNull();
        RuleFor(c => c.CreatedBy).NotEmpty();
    }
}

public sealed class PublishLeavePolicyCommandValidator : AbstractValidator<PublishLeavePolicyCommand>
{
    public PublishLeavePolicyCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.LeavePolicyId).NotEmpty();
        RuleFor(c => c.ActorId).NotEmpty();
    }
}

public sealed class ReviseLeavePolicyCommandValidator : AbstractValidator<ReviseLeavePolicyCommand>
{
    public ReviseLeavePolicyCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.LeavePolicyId).NotEmpty();
        RuleFor(c => c.Ruleset).NotNull();
        RuleFor(c => c.ChangedBy).NotEmpty();
    }
}

public sealed class AssignLeavePolicyCommandValidator : AbstractValidator<AssignLeavePolicyCommand>
{
    public AssignLeavePolicyCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.LeavePolicyId).NotEmpty();
        RuleFor(c => c.PolicyAssignmentId).NotEmpty();
        RuleFor(c => c.ScopeTargetId).NotEmpty().MaximumLength(200);
        RuleFor(c => c.AssignedBy).NotEmpty();
    }
}

public sealed class UnassignLeavePolicyCommandValidator : AbstractValidator<UnassignLeavePolicyCommand>
{
    public UnassignLeavePolicyCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.LeavePolicyId).NotEmpty();
        RuleFor(c => c.PolicyAssignmentId).NotEmpty();
        RuleFor(c => c.ActorId).NotEmpty();
    }
}

public sealed class RetireLeavePolicyCommandValidator : AbstractValidator<RetireLeavePolicyCommand>
{
    public RetireLeavePolicyCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.LeavePolicyId).NotEmpty();
        RuleFor(c => c.ActorId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
    }
}
