using FluentValidation;
using Hris.Modules.Attendance.Application.Commands;

namespace Hris.Modules.Attendance.Application.Validators;

/// <summary>
/// Shape-and-existence validation for <c>AttendanceDevice</c> commands. Active-device and
/// retirement rules (AT-050, AT-051) are enforced by the aggregate; here we confirm the request is
/// well-formed and names the device it must (application/validations.md).
/// </summary>
public sealed class RegisterAttendanceDeviceCommandValidator : AbstractValidator<RegisterAttendanceDeviceCommand>
{
    public RegisterAttendanceDeviceCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.SerialNumber).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Location).NotNull();
        RuleFor(c => c.Configuration).NotNull();
        RuleFor(c => c.ActorId).NotEmpty();
    }
}

public sealed class ConfigureAttendanceDeviceCommandValidator : AbstractValidator<ConfigureAttendanceDeviceCommand>
{
    public ConfigureAttendanceDeviceCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.AttendanceDeviceId).NotEmpty();
        RuleFor(c => c.Configuration).NotNull();
        RuleFor(c => c.ActorId).NotEmpty();
    }
}

public sealed class ChangeAttendanceDeviceStatusCommandValidator : AbstractValidator<ChangeAttendanceDeviceStatusCommand>
{
    public ChangeAttendanceDeviceStatusCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.AttendanceDeviceId).NotEmpty();
    }
}

public sealed class RetireAttendanceDeviceCommandValidator : AbstractValidator<RetireAttendanceDeviceCommand>
{
    public RetireAttendanceDeviceCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.AttendanceDeviceId).NotEmpty();
        RuleFor(c => c.ActorId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
    }
}
