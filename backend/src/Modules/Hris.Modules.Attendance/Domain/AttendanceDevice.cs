using Hris.SharedKernel;

namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// One registered attendance-capture device and its lifecycle. Source:
/// docs/04-modules/attendance/domain/aggregates.md (AttendanceDevice).
///
/// Master data referenced by many <see cref="TimeEvent"/> entries. Only a device in
/// <see cref="DeviceStatus.Active"/> may submit events accepted by
/// <see cref="AttendanceRecord"/> (AT-050); retiring it does not alter the historical
/// events it already captured, which stay attributed to it (AT-051). An automatic
/// offline transition carries no actor (AT-071).
/// </summary>
public sealed class AttendanceDevice : AggregateRoot<AttendanceDeviceId>
{
    public Guid TenantId { get; }

    public string Name { get; private set; }

    public string SerialNumber { get; }

    public string? Manufacturer { get; private set; }

    public string? Model { get; private set; }

    public string? FirmwareVersion { get; private set; }

    public AttendanceDeviceType Type { get; private set; }

    public DeviceLocation Location { get; private set; }

    public DeviceConfiguration Configuration { get; private set; }

    public DeviceStatus Status { get; private set; }

    public DateTimeOffset? LastSynchronizedUtc { get; private set; }

    private AttendanceDevice(
        AttendanceDeviceId id, Guid tenantId, string name, string serialNumber, string? manufacturer, string? model,
        string? firmwareVersion, AttendanceDeviceType type, DeviceLocation location, DeviceConfiguration configuration)
        : base(id)
    {
        TenantId = tenantId;
        Name = name;
        SerialNumber = serialNumber;
        Manufacturer = manufacturer;
        Model = model;
        FirmwareVersion = firmwareVersion;
        Type = type;
        Location = location;
        Configuration = configuration;
        Status = DeviceStatus.Active;
    }

    public static Result<AttendanceDevice> Create(
        AttendanceDeviceId id, Guid tenantId, string? name, string? serialNumber, string? manufacturer, string? model,
        string? firmwareVersion, AttendanceDeviceType type, DeviceLocation location, DeviceConfiguration configuration,
        Guid actorId, DateTimeOffset createdOnUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<AttendanceDevice>(AttendanceErrors.DeviceNameRequired);
        }

        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            return Result.Failure<AttendanceDevice>(AttendanceErrors.DeviceSerialRequired);
        }

        var device = new AttendanceDevice(
            id, tenantId, name.Trim(), serialNumber.Trim(), manufacturer, model, firmwareVersion, type, location,
            configuration);
        device.AddDomainEvent(new AttendanceDeviceRegistered(
            Guid.NewGuid(), createdOnUtc, id, tenantId, actorId));
        return Result.Success(device);
    }

    /// <summary>True only when this device may submit events accepted by a record (AT-050).</summary>
    public bool CanSubmitEvents() => Status == DeviceStatus.Active;

    public Result Configure(DeviceConfiguration configuration, Guid actorId, DateTimeOffset nowUtc)
    {
        if (Status == DeviceStatus.Retired)
        {
            return Result.Failure(AttendanceErrors.DeviceNotActive);
        }

        Configuration = configuration;
        AddDomainEvent(new AttendanceDeviceConfigured(Guid.NewGuid(), nowUtc, Id, TenantId, actorId));
        return Result.Success();
    }

    /// <summary>An automatic status change (e.g. offline detection) passes no actor (AT-071).</summary>
    public Result ChangeStatus(DeviceStatus newStatus, Guid? actorId, DateTimeOffset nowUtc)
    {
        if (newStatus == Status)
        {
            return Result.Success();
        }

        Status = newStatus;
        if (newStatus == DeviceStatus.Active)
        {
            LastSynchronizedUtc = nowUtc;
        }

        AddDomainEvent(new AttendanceDeviceStatusChanged(Guid.NewGuid(), nowUtc, Id, TenantId, newStatus, actorId));
        return Result.Success();
    }

    public Result Retire(Guid actorId, string reason, DateTimeOffset nowUtc)
    {
        if (Status == DeviceStatus.Retired)
        {
            return Result.Success();
        }

        Status = DeviceStatus.Retired;
        AddDomainEvent(new AttendanceDeviceRetired(Guid.NewGuid(), nowUtc, Id, TenantId, actorId, reason));
        return Result.Success();
    }
}
