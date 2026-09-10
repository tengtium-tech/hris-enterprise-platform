using FluentValidation;
using Hris.Modules.Timekeeping.Application.Commands;

namespace Hris.Modules.Timekeeping.Application.Validators;

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
