using FluentAssertions;
using FluentValidation;
using Hris.Modules.Attendance.Application.Commands;
using Hris.Modules.Attendance.Application.Queries;
using Hris.Modules.Attendance.Domain;
using Hris.Testing.TenantIsolation;
using MediatR;
using Xunit;

namespace Hris.Modules.Attendance.Tests.Application;

/// <summary>
/// AttendanceDevice's command and query handlers, dispatched through the real MediatR
/// pipeline against the shared tenant-isolation harness (HEP-111).
/// </summary>
public sealed class AttendanceDeviceApplicationTests : TenantIsolationTestBase
{
    private readonly Guid _tenantId = Guid.NewGuid();

    public AttendanceDeviceApplicationTests(TenantIsolationFixture fixture)
        : base(fixture)
    {
    }

    private ISender Sender => GetService<ISender>();

    private async Task<Guid> RegisterAndReturnIdAsync() =>
        (await Sender.Send(new RegisterAttendanceDeviceCommand(
            _tenantId, "Main Entrance", "SN-001", "ZKTeco", "F18", "1.2.0", AttendanceDeviceType.Biometric,
            DeviceLocation.FromReference("building-1"), new DeviceConfiguration("Asia/Manila", 60, true),
            Guid.NewGuid())).ConfigureAwait(false)).Value;

    [Fact]
    public async Task Register_CreatesAnActiveDevice_RetrievableByQuery()
    {
        var deviceId = await RegisterAndReturnIdAsync();

        var result = await Sender.Send(new GetAttendanceDeviceQuery(_tenantId, deviceId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(nameof(DeviceStatus.Active));
        result.Value.SerialNumber.Should().Be("SN-001");
        result.Value.Configuration!.TimeZoneId.Should().Be("Asia/Manila");
    }

    [Fact]
    public async Task Register_Fails_Validation_WhenNameIsEmpty()
    {
        var act = () => Sender.Send(new RegisterAttendanceDeviceCommand(
            _tenantId, string.Empty, "SN-001", null, null, null, AttendanceDeviceType.Biometric,
            DeviceLocation.FromReference("loc"), new DeviceConfiguration(null, null, null), Guid.NewGuid()));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Configure_UpdatesTheConfiguration()
    {
        var deviceId = await RegisterAndReturnIdAsync();
        var newConfiguration = new DeviceConfiguration("America/New_York", 30, false);

        var result = await Sender.Send(new ConfigureAttendanceDeviceCommand(_tenantId, deviceId, newConfiguration, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        var device = (await Sender.Send(new GetAttendanceDeviceQuery(_tenantId, deviceId))).Value;
        device.Configuration!.TimeZoneId.Should().Be("America/New_York");
        device.Configuration.HeartbeatIntervalSeconds.Should().Be(30);
    }

    [Fact]
    public async Task Configure_Fails_WhenRetired()
    {
        var deviceId = await RegisterAndReturnIdAsync();
        await Sender.Send(new RetireAttendanceDeviceCommand(_tenantId, deviceId, Guid.NewGuid(), "decommissioned"));

        var result = await Sender.Send(new ConfigureAttendanceDeviceCommand(
            _tenantId, deviceId, new DeviceConfiguration(null, null, null), Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.DeviceNotActive);
    }

    [Fact]
    public async Task ChangeStatus_AllowsNoActor_ForAnAutomaticTransition()
    {
        // AT-071: an automatic offline transition was not decided by a person.
        var deviceId = await RegisterAndReturnIdAsync();

        var result = await Sender.Send(new ChangeAttendanceDeviceStatusCommand(_tenantId, deviceId, DeviceStatus.Offline, null));

        result.IsSuccess.Should().BeTrue();
        var device = (await Sender.Send(new GetAttendanceDeviceQuery(_tenantId, deviceId))).Value;
        device.Status.Should().Be(nameof(DeviceStatus.Offline));
    }

    [Fact]
    public async Task Retire_TransitionsToRetired()
    {
        var deviceId = await RegisterAndReturnIdAsync();

        var result = await Sender.Send(new RetireAttendanceDeviceCommand(_tenantId, deviceId, Guid.NewGuid(), "decommissioned"));

        result.IsSuccess.Should().BeTrue();
        var device = (await Sender.Send(new GetAttendanceDeviceQuery(_tenantId, deviceId))).Value;
        device.Status.Should().Be(nameof(DeviceStatus.Retired));
    }

    [Fact]
    public async Task GetAttendanceDeviceQuery_ReturnsNotFound_ForAnotherTenantsDevice()
    {
        var deviceId = await RegisterAndReturnIdAsync();

        var result = await Sender.Send(new GetAttendanceDeviceQuery(Guid.NewGuid(), deviceId));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.AttendanceDeviceNotFound);
    }
}
