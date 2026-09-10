using FluentValidation;
using Hris.Modules.Timekeeping.Application.Commands;

namespace Hris.Modules.Timekeeping.Application.Validators;

public sealed class CreateShiftAssignmentCommandValidator : AbstractValidator<CreateShiftAssignmentCommand>
{
    public CreateShiftAssignmentCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.TargetId).NotEmpty().MaximumLength(200);
        RuleFor(c => c.WorkShiftId).NotEmpty();
        RuleFor(c => c.EffectiveTo)
            .GreaterThanOrEqualTo(c => c.EffectiveFrom)
            .When(c => c.EffectiveTo is not null);

        // TK-032 at the shape level as well as in the aggregate, so a caller learns
        // before dispatch rather than after.
        RuleFor(c => c.EffectiveTo).NotNull().When(c => c.IsTemporary);
    }
}

public sealed class CancelShiftAssignmentCommandValidator : AbstractValidator<CancelShiftAssignmentCommand>
{
    public CancelShiftAssignmentCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.ShiftAssignmentId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty().MaximumLength(500);
        RuleFor(c => c.ActingUser).NotEmpty();
    }
}

public sealed class ProposeShiftSwapCommandValidator : AbstractValidator<ProposeShiftSwapCommand>
{
    public ProposeShiftSwapCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.PrimaryAssignmentId).NotEmpty();
        RuleFor(c => c.SecondaryAssignmentId).NotEmpty().NotEqual(c => c.PrimaryAssignmentId);
        RuleFor(c => c.ActingUser).NotEmpty();
    }
}

public sealed class ConsentToShiftSwapCommandValidator : AbstractValidator<ConsentToShiftSwapCommand>
{
    public ConsentToShiftSwapCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.PrimaryAssignmentId).NotEmpty();
        RuleFor(c => c.ConsentingEmployeeId).NotEmpty();
    }
}

public sealed class OverrideShiftSwapCommandValidator : AbstractValidator<OverrideShiftSwapCommand>
{
    public OverrideShiftSwapCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.PrimaryAssignmentId).NotEmpty();
        RuleFor(c => c.ActingUser).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class ActivateShiftSwapCommandValidator : AbstractValidator<ActivateShiftSwapCommand>
{
    public ActivateShiftSwapCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.PrimaryAssignmentId).NotEmpty();
    }
}
