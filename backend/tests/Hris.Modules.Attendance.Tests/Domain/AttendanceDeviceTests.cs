using FluentAssertions;
using Hris.Modules.Attendance.Domain;
using Xunit;

namespace Hris.Modules.Attendance.Tests.Domain;

/// <summary>
/// AT-050, AT-051: master data referenced by many <see cref="TimeEvent"/> entries. Only
/// an <see cref="DeviceStatus.Active"/> device may submit accepted events; retiring a
/// device never rewrites the events it already captured.
/// </summary>
public sealed class AttendanceDeviceTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    private AttendanceDevice NewDevice() =>
        AttendanceDevice.Create(
            new AttendanceDeviceId(Guid.NewGuid()), _tenantId, "Main Entrance", "SN-001", "ZKTeco", "F18",
            "1.2.0", AttendanceDeviceType.Biometric, DeviceLocation.FromReference("building-1"),
            new DeviceConfiguration("Asia/Manila", 60, true), Guid.NewGuid(), TestAttendance.NowUtc).Value;

    [Fact]
    public void Create_StartsActive_AndRaisesRegisteredEvent()
    {
        var device = NewDevice();

        device.Status.Should().Be(DeviceStatus.Active);
        device.CanSubmitEvents().Should().BeTrue();
        device.DomainEvents.OfType<AttendanceDeviceRegistered>().Should().ContainSingle();
    }

    [Theory]
    [InlineData("", "SN-001")]
    [InlineData("   ", "SN-001")]
    public void Create_Fails_WhenNameIsMissing(string name, string serial)
    {
        var result = AttendanceDevice.Create(
            new AttendanceDeviceId(Guid.NewGuid()), _tenantId, name, serial, null, null, null,
            AttendanceDeviceType.Biometric, DeviceLocation.FromReference("loc"), default, Guid.NewGuid(), TestAttendance.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.DeviceNameRequired);
    }

    [Theory]
    [InlineData("Main Entrance", "")]
    [InlineData("Main Entrance", "   ")]
    public void Create_Fails_WhenSerialNumberIsMissing(string name, string serial)
    {
        var result = AttendanceDevice.Create(
            new AttendanceDeviceId(Guid.NewGuid()), _tenantId, name, serial, null, null, null,
            AttendanceDeviceType.Biometric, DeviceLocation.FromReference("loc"), default, Guid.NewGuid(), TestAttendance.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.DeviceSerialRequired);
    }

    [Fact]
    public void CanSubmitEvents_IsFalse_ForAnyNonActiveStatus()
    {
        var device = NewDevice();
        device.ChangeStatus(DeviceStatus.Offline, null, TestAttendance.NowUtc);

        device.CanSubmitEvents().Should().BeFalse();
    }

    [Fact]
    public void Configure_UpdatesTheConfiguration_AndRaisesConfiguredEvent()
    {
        var device = NewDevice();
        var newConfiguration = new DeviceConfiguration("America/New_York", 30, false);

        var result = device.Configure(newConfiguration, Guid.NewGuid(), TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        device.Configuration.Should().Be(newConfiguration);
        device.DomainEvents.OfType<AttendanceDeviceConfigured>().Should().ContainSingle();
    }

    [Fact]
    public void Configure_Fails_WhenRetired()
    {
        var device = NewDevice();
        device.Retire(Guid.NewGuid(), "decommissioned", TestAttendance.NowUtc);

        var result = device.Configure(new DeviceConfiguration(null, null, null), Guid.NewGuid(), TestAttendance.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.DeviceNotActive);
    }

    [Fact]
    public void ChangeStatus_IsIdempotent_WhenTheStatusIsUnchanged()
    {
        var device = NewDevice();
        device.ClearDomainEvents();

        var result = device.ChangeStatus(DeviceStatus.Active, Guid.NewGuid(), TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        device.DomainEvents.Should().BeEmpty("no real transition happened");
    }

    [Fact]
    public void ChangeStatus_AllowsNoActor_ForAnAutomaticTransition()
    {
        // AT-071: an automatic offline transition was not decided by a person.
        var device = NewDevice();

        var result = device.ChangeStatus(DeviceStatus.Offline, null, TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        device.DomainEvents.OfType<AttendanceDeviceStatusChanged>().Single().ActorId.Should().BeNull();
    }

    [Fact]
    public void ChangeStatus_UpdatesLastSynchronized_WhenReturningToActive()
    {
        var device = NewDevice();
        device.ChangeStatus(DeviceStatus.Offline, null, TestAttendance.NowUtc);

        var reconnectedAt = TestAttendance.NowUtc.AddHours(1);
        device.ChangeStatus(DeviceStatus.Active, Guid.NewGuid(), reconnectedAt);

        device.LastSynchronizedUtc.Should().Be(reconnectedAt);
    }

    [Fact]
    public void Retire_TransitionsToRetired_AndRaisesRetiredEvent()
    {
        var device = NewDevice();

        var result = device.Retire(Guid.NewGuid(), "decommissioned", TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        device.Status.Should().Be(DeviceStatus.Retired);
        device.DomainEvents.OfType<AttendanceDeviceRetired>().Should().ContainSingle();
    }

    [Fact]
    public void Retire_IsIdempotent_WhenAlreadyRetired()
    {
        var device = NewDevice();
        device.Retire(Guid.NewGuid(), "first", TestAttendance.NowUtc);
        device.ClearDomainEvents();

        var result = device.Retire(Guid.NewGuid(), "second", TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        device.DomainEvents.Should().BeEmpty();
    }
}
