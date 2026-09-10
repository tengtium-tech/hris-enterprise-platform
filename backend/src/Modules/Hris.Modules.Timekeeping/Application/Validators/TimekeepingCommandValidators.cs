using FluentValidation;
using Hris.Modules.Timekeeping.Application.Commands;

namespace Hris.Modules.Timekeeping.Application.Validators;

/// <summary>
/// Shape validation only. Every temporal and structural rule — TK-001's immutability,
/// TK-002's version selection, TK-020's anchor requirement, TK-030's precedence,
/// TK-041's country-layer protection — is decided by the aggregate, not here.
/// Duplicating any of them in a validator would create a second place the rule could
/// drift from.
/// </summary>
public sealed class DefineWorkScheduleCommandValidator : AbstractValidator<DefineWorkScheduleCommand>
{
    public DefineWorkScheduleCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).MaximumLength(2000);
        RuleFor(c => c.WorkingDays).NotEmpty();
        RuleFor(c => c.ActingUser).NotEmpty();
    }
}

public sealed class PublishWorkScheduleCommandValidator : AbstractValidator<PublishWorkScheduleCommand>
{
    public PublishWorkScheduleCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.WorkScheduleId).NotEmpty();
        RuleFor(c => c.ActingUser).NotEmpty();
    }
}

public sealed class ReviseWorkScheduleCommandValidator : AbstractValidator<ReviseWorkScheduleCommand>
{
    public ReviseWorkScheduleCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.WorkScheduleId).NotEmpty();
        RuleFor(c => c.WorkingDays).NotEmpty();
        RuleFor(c => c.ActingUser).NotEmpty();
    }
}

public sealed class RetireWorkScheduleCommandValidator : AbstractValidator<RetireWorkScheduleCommand>
{
    public RetireWorkScheduleCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.WorkScheduleId).NotEmpty();
        RuleFor(c => c.ActingUser).NotEmpty();
    }
}

public sealed class AssignWorkScheduleCommandValidator : AbstractValidator<AssignWorkScheduleCommand>
{
    public AssignWorkScheduleCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.WorkScheduleId).NotEmpty();
        RuleFor(c => c.TargetId).NotEmpty().MaximumLength(200);
        RuleFor(c => c.ActingUser).NotEmpty();
        RuleFor(c => c.EffectiveTo)
            .GreaterThanOrEqualTo(c => c.EffectiveFrom)
            .When(c => c.EffectiveTo is not null);
    }
}

public sealed class UnassignWorkScheduleCommandValidator : AbstractValidator<UnassignWorkScheduleCommand>
{
    public UnassignWorkScheduleCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.WorkScheduleId).NotEmpty();
        RuleFor(c => c.ScheduleAssignmentId).NotEmpty();
        RuleFor(c => c.ActingUser).NotEmpty();
    }
}

public sealed class DefineWorkShiftCommandValidator : AbstractValidator<DefineWorkShiftCommand>
{
    public DefineWorkShiftCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Code).NotEmpty().MaximumLength(32);
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Timing).NotNull();
        RuleFor(c => c.ActingUser).NotEmpty();
    }
}

public sealed class PublishWorkShiftCommandValidator : AbstractValidator<PublishWorkShiftCommand>
{
    public PublishWorkShiftCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.WorkShiftId).NotEmpty();
        RuleFor(c => c.ActingUser).NotEmpty();
    }
}

public sealed class ReviseWorkShiftCommandValidator : AbstractValidator<ReviseWorkShiftCommand>
{
    public ReviseWorkShiftCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.WorkShiftId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Timing).NotNull();
        RuleFor(c => c.ActingUser).NotEmpty();
    }
}

public sealed class RetireWorkShiftCommandValidator : AbstractValidator<RetireWorkShiftCommand>
{
    public RetireWorkShiftCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.WorkShiftId).NotEmpty();
        RuleFor(c => c.ActingUser).NotEmpty();
    }
}

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

public sealed class DefineHolidayCalendarCommandValidator : AbstractValidator<DefineHolidayCalendarCommand>
{
    public DefineHolidayCalendarCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.ScopeTargetId).NotEmpty().MaximumLength(200);
        RuleFor(c => c.CountryCode).NotEmpty().MaximumLength(8);
        RuleFor(c => c.ActingUser).NotEmpty();
    }
}

public sealed class AddHolidayCommandValidator : AbstractValidator<AddHolidayCommand>
{
    public AddHolidayCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.HolidayCalendarId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.ActingUser).NotEmpty();
    }
}

public sealed class RemoveHolidayCommandValidator : AbstractValidator<RemoveHolidayCommand>
{
    public RemoveHolidayCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.HolidayCalendarId).NotEmpty();
        RuleFor(c => c.HolidayId).NotEmpty();
        RuleFor(c => c.ActingUser).NotEmpty();
    }
}

public sealed class PublishHolidayCalendarCommandValidator : AbstractValidator<PublishHolidayCalendarCommand>
{
    public PublishHolidayCalendarCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.HolidayCalendarId).NotEmpty();
        RuleFor(c => c.ActingUser).NotEmpty();
    }
}

public sealed class ReviseHolidayCalendarCommandValidator : AbstractValidator<ReviseHolidayCalendarCommand>
{
    public ReviseHolidayCalendarCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.HolidayCalendarId).NotEmpty();
        RuleFor(c => c.ActingUser).NotEmpty();
    }
}

public sealed class ChangeHolidayCalendarLayerCommandValidator : AbstractValidator<ChangeHolidayCalendarLayerCommand>
{
    public ChangeHolidayCalendarLayerCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.HolidayCalendarId).NotEmpty();
        RuleFor(c => c.ActingUser).NotEmpty();
    }
}
