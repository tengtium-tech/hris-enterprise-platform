using FluentValidation;
using Hris.Modules.Leave.Application.Commands;

namespace Hris.Modules.Leave.Application.Validators;

/// <summary>
/// Shape-and-existence validation for <c>LeaveType</c> commands. Statutory-identity
/// protection (LV-001, LV-004) belongs to the aggregate; here we confirm the request
/// is well-formed (application/validations.md).
/// </summary>
public sealed class DefineLeaveTypeCommandValidator : AbstractValidator<DefineLeaveTypeCommand>
{
    public DefineLeaveTypeCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Code).NotEmpty().MaximumLength(50);
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.ActorId).NotEmpty();
    }
}

public sealed class DeactivateLeaveTypeCommandValidator : AbstractValidator<DeactivateLeaveTypeCommand>
{
    public DeactivateLeaveTypeCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.LeaveTypeId).NotEmpty();
        RuleFor(c => c.ActorId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
    }
}
