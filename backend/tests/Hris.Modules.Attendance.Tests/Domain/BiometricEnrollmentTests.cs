using FluentAssertions;
using Hris.Modules.Attendance.Domain;
using Xunit;

namespace Hris.Modules.Attendance.Tests.Domain;

/// <summary>
/// AT-052, AT-053: only an encrypted, non-reversible template reference is stored, and
/// revocation is not complete until every device holding a synchronized copy confirms
/// it -- <see cref="BiometricEnrollment.Revoke"/> moves the synchronized set into a
/// pending-revocation set, and each <see cref="BiometricEnrollment.ConfirmDeviceRevocation"/>
/// clears one until the set is empty.
/// </summary>
public sealed class BiometricEnrollmentTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _employeeId = Guid.NewGuid();

    private BiometricEnrollment NewEnrollment(bool consentConfirmed = true) =>
        BiometricEnrollment.Enroll(
            new BiometricEnrollmentId(Guid.NewGuid()), _tenantId, _employeeId, BiometricMethod.Fingerprint,
            "encrypted-template-pointer", consentConfirmed, "ZKTeco", Guid.NewGuid(), TestAttendance.NowUtc).Value;

    // ---- Enroll --------------------------------------------------------

    [Fact]
    public void Enroll_StartsAsEnrolled_AndRaisesCreatedEvent()
    {
        var enrollment = NewEnrollment();

        enrollment.Status.Should().Be(EnrollmentStatus.Enrolled);
        enrollment.IsActive().Should().BeFalse("Enrolled is not yet Active");
        enrollment.DomainEvents.OfType<BiometricEnrollmentCreated>().Should().ContainSingle();
    }

    [Fact]
    public void Enroll_Fails_WhenConsentIsNotConfirmed()
    {
        var result = BiometricEnrollment.Enroll(
            new BiometricEnrollmentId(Guid.NewGuid()), _tenantId, _employeeId, BiometricMethod.Fingerprint,
            "pointer", consentConfirmed: false, null, Guid.NewGuid(), TestAttendance.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.BiometricConsentRequired);
    }

    [Fact]
    public void Enroll_Fails_WhenEmployeeIdIsEmpty()
    {
        var result = BiometricEnrollment.Enroll(
            new BiometricEnrollmentId(Guid.NewGuid()), _tenantId, Guid.Empty, BiometricMethod.Fingerprint,
            "pointer", true, null, Guid.NewGuid(), TestAttendance.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.EmployeeIdentifierRequired);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Enroll_Fails_WhenTheTemplatePointerIsMissing(string? templateReference)
    {
        var result = BiometricEnrollment.Enroll(
            new BiometricEnrollmentId(Guid.NewGuid()), _tenantId, _employeeId, BiometricMethod.Fingerprint,
            templateReference, true, null, Guid.NewGuid(), TestAttendance.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.BiometricTemplateReferenceRequired);
    }

    // ---- Activate ------------------------------------------------------

    [Fact]
    public void Activate_TransitionsToActive_FromEnrolled()
    {
        var enrollment = NewEnrollment();

        var result = enrollment.Activate(Guid.NewGuid(), TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        enrollment.Status.Should().Be(EnrollmentStatus.Active);
        enrollment.IsActive().Should().BeTrue();
    }

    [Fact]
    public void Activate_IsIdempotent_WhenAlreadyActive()
    {
        var enrollment = NewEnrollment();
        enrollment.Activate(Guid.NewGuid(), TestAttendance.NowUtc);
        enrollment.ClearDomainEvents();

        var result = enrollment.Activate(Guid.NewGuid(), TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        enrollment.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Activate_Fails_WhenRevoked()
    {
        var enrollment = NewEnrollment();
        enrollment.Revoke(Guid.NewGuid(), "no longer employed", TestAttendance.NowUtc);

        var result = enrollment.Activate(Guid.NewGuid(), TestAttendance.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.BiometricEnrollmentNotActive);
    }

    // ---- Revoke / ConfirmDeviceRevocation (AT-053) -----------------------

    [Fact]
    public void Revoke_CompletesImmediately_WhenNoDeviceHoldsASynchronizedCopy()
    {
        var enrollment = NewEnrollment();
        var actorId = Guid.NewGuid();

        var result = enrollment.Revoke(actorId, "employee separated", TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        enrollment.Status.Should().Be(EnrollmentStatus.Revoked);
        var raised = enrollment.DomainEvents.OfType<BiometricEnrollmentRevoked>().Single();
        raised.ActorId.Should().Be(actorId);
    }

    [Fact]
    public void Revoke_StaysPending_WhileASynchronizedDeviceHasNotYetConfirmed()
    {
        var enrollment = NewEnrollment();
        var deviceId = Guid.NewGuid();
        enrollment.AddSynchronizedDevice(deviceId);

        var result = enrollment.Revoke(Guid.NewGuid(), "reason", TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        enrollment.Status.Should().NotBe(EnrollmentStatus.Revoked, "revocation is not complete until every device confirms (AT-053)");
        enrollment.PendingRevocationDeviceIds.Should().ContainSingle().Which.Should().Be(deviceId);
        enrollment.SynchronizedDeviceIds.Should().BeEmpty("the device moved to the pending-revocation set");
        enrollment.DomainEvents.OfType<BiometricEnrollmentRevoked>().Should().BeEmpty();
    }

    [Fact]
    public void ConfirmDeviceRevocation_CompletesRevocation_OnceEveryDeviceHasConfirmed_WithNoActor()
    {
        // AT-071: the completing confirmation is a device callback, not a person's decision.
        var enrollment = NewEnrollment();
        var deviceA = Guid.NewGuid();
        var deviceB = Guid.NewGuid();
        enrollment.AddSynchronizedDevice(deviceA);
        enrollment.AddSynchronizedDevice(deviceB);
        enrollment.Revoke(Guid.NewGuid(), "reason", TestAttendance.NowUtc);

        enrollment.ConfirmDeviceRevocation(deviceA, TestAttendance.NowUtc);
        enrollment.Status.Should().NotBe(EnrollmentStatus.Revoked, "device B has not confirmed yet");

        var result = enrollment.ConfirmDeviceRevocation(deviceB, TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        enrollment.Status.Should().Be(EnrollmentStatus.Revoked);
        enrollment.DomainEvents.OfType<BiometricEnrollmentRevoked>().Single().ActorId.Should().BeNull();
    }

    [Fact]
    public void ConfirmDeviceRevocation_IsIdempotent_WhenAlreadyRevoked()
    {
        var enrollment = NewEnrollment();
        enrollment.Revoke(Guid.NewGuid(), "reason", TestAttendance.NowUtc);
        enrollment.ClearDomainEvents();

        var result = enrollment.ConfirmDeviceRevocation(Guid.NewGuid(), TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        enrollment.DomainEvents.Should().BeEmpty();
    }

    // ---- AddSynchronizedDevice ---------------------------------------

    [Fact]
    public void AddSynchronizedDevice_IsIdempotent_ForTheSameDeviceTwice()
    {
        var enrollment = NewEnrollment();
        var deviceId = Guid.NewGuid();

        enrollment.AddSynchronizedDevice(deviceId);
        enrollment.AddSynchronizedDevice(deviceId);

        enrollment.SynchronizedDeviceIds.Should().ContainSingle();
    }

    [Fact]
    public void AddSynchronizedDevice_Fails_WhenRevoked()
    {
        var enrollment = NewEnrollment();
        enrollment.Revoke(Guid.NewGuid(), "reason", TestAttendance.NowUtc);

        var result = enrollment.AddSynchronizedDevice(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.BiometricEnrollmentNotActive);
    }
}
