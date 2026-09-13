using Hris.Application.Abstractions;
using Hris.Modules.Attendance.Application;
using Hris.Modules.Attendance.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Attendance.Application.Commands;

/// <summary>Registers a capture device. Source: application/commands.md (AttendanceDevice Commands).</summary>
public sealed record RegisterAttendanceDeviceCommand(
    Guid TenantId,
    string Name,
    string SerialNumber,
    string? Manufacturer,
    string? Model,
    string? FirmwareVersion,
    AttendanceDeviceType Type,
    DeviceLocation Location,
    DeviceConfiguration Configuration,
    Guid ActorId) : ICommand<Result<Guid>>;

internal sealed class RegisterAttendanceDeviceCommandHandler
    : IRequestHandler<RegisterAttendanceDeviceCommand, Result<Guid>>
{
    private readonly IAttendanceDeviceRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RegisterAttendanceDeviceCommandHandler(IAttendanceDeviceRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(RegisterAttendanceDeviceCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var createResult = AttendanceDevice.Create(
            new AttendanceDeviceId(Guid.NewGuid()),
            request.TenantId,
            request.Name,
            request.SerialNumber,
            request.Manufacturer,
            request.Model,
            request.FirmwareVersion,
            request.Type,
            request.Location,
            request.Configuration,
            request.ActorId,
            _timeProvider.GetUtcNow());

        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Error);
        }

        await _repository.AddAsync(createResult.Value, cancellationToken).ConfigureAwait(false);
        return Result.Success(createResult.Value.Id.Value);
    }
}

/// <summary>Updates a device's operational configuration.</summary>
public sealed record ConfigureAttendanceDeviceCommand(
    Guid TenantId, Guid AttendanceDeviceId, DeviceConfiguration Configuration, Guid ActorId) : ICommand<Result>;

internal sealed class ConfigureAttendanceDeviceCommandHandler : IRequestHandler<ConfigureAttendanceDeviceCommand, Result>
{
    private readonly IAttendanceDeviceRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ConfigureAttendanceDeviceCommandHandler(IAttendanceDeviceRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ConfigureAttendanceDeviceCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var deviceResult = await AttendanceLookup
            .LoadDeviceForTenantAsync(_repository, request.AttendanceDeviceId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (deviceResult.IsFailure)
        {
            return Result.Failure(deviceResult.Error);
        }

        return deviceResult.Value.Configure(request.Configuration, request.ActorId, _timeProvider.GetUtcNow());
    }
}

/// <summary>Changes a device's status. An automatic transition (offline detection) carries no actor (AT-071).</summary>
public sealed record ChangeAttendanceDeviceStatusCommand(
    Guid TenantId, Guid AttendanceDeviceId, DeviceStatus NewStatus, Guid? ActorId) : ICommand<Result>;

internal sealed class ChangeAttendanceDeviceStatusCommandHandler
    : IRequestHandler<ChangeAttendanceDeviceStatusCommand, Result>
{
    private readonly IAttendanceDeviceRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ChangeAttendanceDeviceStatusCommandHandler(IAttendanceDeviceRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ChangeAttendanceDeviceStatusCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var deviceResult = await AttendanceLookup
            .LoadDeviceForTenantAsync(_repository, request.AttendanceDeviceId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (deviceResult.IsFailure)
        {
            return Result.Failure(deviceResult.Error);
        }

        return deviceResult.Value.ChangeStatus(request.NewStatus, request.ActorId, _timeProvider.GetUtcNow());
    }
}

/// <summary>Retires a device; its historical events stay attributed to it (AT-051).</summary>
public sealed record RetireAttendanceDeviceCommand(Guid TenantId, Guid AttendanceDeviceId, Guid ActorId, string Reason)
    : ICommand<Result>;

internal sealed class RetireAttendanceDeviceCommandHandler : IRequestHandler<RetireAttendanceDeviceCommand, Result>
{
    private readonly IAttendanceDeviceRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RetireAttendanceDeviceCommandHandler(IAttendanceDeviceRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RetireAttendanceDeviceCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var deviceResult = await AttendanceLookup
            .LoadDeviceForTenantAsync(_repository, request.AttendanceDeviceId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (deviceResult.IsFailure)
        {
            return Result.Failure(deviceResult.Error);
        }

        return deviceResult.Value.Retire(request.ActorId, request.Reason, _timeProvider.GetUtcNow());
    }
}
