using FluentValidation;
using Hris.Modules.Timekeeping.Application.Commands;

namespace Hris.Modules.Timekeeping.Application.Validators;

/// <summary>
/// Shape validation only. Every temporal and structural rule — TK-001's immutability,
/// TK-002's version selection, TK-020's anchor requirement, TK-030's precedence,
/// TK-041's country-layer protection — is decided by the aggregate, not here.
/// Duplicating any of them in a validator would create a second place the rule could
/// drift from. That applies to every validator in this folder, not only this file.
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
