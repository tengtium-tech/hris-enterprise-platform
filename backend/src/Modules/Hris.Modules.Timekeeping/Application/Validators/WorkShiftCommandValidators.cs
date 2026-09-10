using FluentValidation;
using Hris.Modules.Timekeeping.Application.Commands;

namespace Hris.Modules.Timekeeping.Application.Validators;

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
