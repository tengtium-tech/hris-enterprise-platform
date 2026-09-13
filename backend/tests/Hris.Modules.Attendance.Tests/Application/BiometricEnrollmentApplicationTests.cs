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
/// BiometricEnrollment's command and query handlers, dispatched through the real MediatR
/// pipeline against the shared tenant-isolation harness (HEP-111). AT-053's own rule --
/// revocation is not complete until every synchronized device confirms -- has no
/// application command wrapping the device-callback confirmation itself (it is issued by
/// the device-integration layer, never by a caller), so the pending-revocation scenario
/// here seeds an aggregate carrying a synchronized device directly through the domain's
/// own public API, the same way <see cref="AttendanceAdjustmentApplicationTests"/> seeds a
/// Submitted adjustment it cannot reach through any command either.
/// </summary>
public sealed class BiometricEnrollmentApplicationTests : TenantIsolationTestBase
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _employeeId = Guid.NewGuid();

    public BiometricEnrollmentApplicationTests(TenantIsolationFixture fixture)
        : base(fixture)
    {
    }

    private ISender Sender => GetService<ISender>();

    private async Task<Guid> EnrollAndReturnIdAsync() =>
        (await Sender.Send(new EnrollBiometricCommand(
            _tenantId, _employeeId, BiometricMethod.Fingerprint, "encrypted-template-pointer", true, "ZKTeco",
            Guid.NewGuid())).ConfigureAwait(false)).Value;

    [Fact]
    public async Task Enroll_CreatesAnEnrollment_RetrievableByQuery()
    {
        var enrollmentId = await EnrollAndReturnIdAsync();

        var result = await Sender.Send(new GetBiometricEnrollmentStatusQuery(_tenantId, enrollmentId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(nameof(EnrollmentStatus.Enrolled));
        result.Value.Method.Should().Be(nameof(BiometricMethod.Fingerprint));
    }

    [Fact]
    public async Task Enroll_Fails_WhenConsentIsNotConfirmed()
    {
        var result = await Sender.Send(new EnrollBiometricCommand(
            _tenantId, _employeeId, BiometricMethod.Fingerprint, "pointer", false, null, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.BiometricConsentRequired);
    }

    [Fact]
    public async Task Enroll_Fails_Validation_WhenActorIdIsEmpty()
    {
        var act = () => Sender.Send(new EnrollBiometricCommand(
            _tenantId, _employeeId, BiometricMethod.Fingerprint, "pointer", true, null, Guid.Empty));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Activate_TransitionsToActive()
    {
        var enrollmentId = await EnrollAndReturnIdAsync();

        var result = await Sender.Send(new ActivateBiometricEnrollmentCommand(_tenantId, enrollmentId, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        var enrollment = (await Sender.Send(new GetBiometricEnrollmentStatusQuery(_tenantId, enrollmentId))).Value;
        enrollment.Status.Should().Be(nameof(EnrollmentStatus.Active));
    }

    [Fact]
    public async Task Revoke_CompletesImmediately_WhenNoDeviceHoldsASynchronizedCopy()
    {
        var enrollmentId = await EnrollAndReturnIdAsync();

        var result = await Sender.Send(new RevokeBiometricEnrollmentCommand(_tenantId, enrollmentId, Guid.NewGuid(), "employee separated"));

        result.IsSuccess.Should().BeTrue();
        var enrollment = (await Sender.Send(new GetBiometricEnrollmentStatusQuery(_tenantId, enrollmentId))).Value;
        enrollment.Status.Should().Be(nameof(EnrollmentStatus.Revoked));
    }

    [Fact]
    public async Task Revoke_StaysPending_WhileASynchronizedDeviceHasNotYetConfirmed()
    {
        var enrollment = BiometricEnrollment.Enroll(
            new BiometricEnrollmentId(Guid.NewGuid()), _tenantId, _employeeId, BiometricMethod.Fingerprint,
            "encrypted-template-pointer", true, "ZKTeco", Guid.NewGuid(), TestAttendance.NowUtc).Value;
        enrollment.AddSynchronizedDevice(Guid.NewGuid());
        await SeedAsync(enrollment);

        var result = await Sender.Send(new RevokeBiometricEnrollmentCommand(
            _tenantId, enrollment.Id.Value, Guid.NewGuid(), "employee separated"));

        result.IsSuccess.Should().BeTrue();
        var status = (await Sender.Send(new GetBiometricEnrollmentStatusQuery(_tenantId, enrollment.Id.Value))).Value;
        status.Status.Should().NotBe(nameof(EnrollmentStatus.Revoked), "revocation is not complete until every device confirms (AT-053)");
        status.SynchronizedDeviceIds.Should().BeEmpty("the device moved to the pending-revocation set");
    }

    [Fact]
    public async Task GetBiometricEnrollmentStatusQuery_ReturnsNotFound_ForAnotherTenantsEnrollment()
    {
        var enrollmentId = await EnrollAndReturnIdAsync();

        var result = await Sender.Send(new GetBiometricEnrollmentStatusQuery(Guid.NewGuid(), enrollmentId));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.BiometricEnrollmentNotFound);
    }
}
