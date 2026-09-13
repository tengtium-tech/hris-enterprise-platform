using Hris.SharedKernel;

namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// One employee's biometric registration for one method, an independent aggregate that
/// must outlive any single <see cref="AttendanceDevice"/>. Source:
/// docs/04-modules/attendance/domain/aggregates.md (BiometricEnrollment).
///
/// Only an encrypted, non-reversible <see cref="BiometricTemplateReference"/> is stored
/// (AT-052) — never raw biometric bytes. Revocation is not complete until every device
/// that holds a synchronized copy confirms it (AT-053): <see cref="Revoke"/> moves the
/// synchronized device set into a pending-revocation set, and each
/// <see cref="ConfirmDeviceRevocation"/> clears one; when the pending set is empty the
/// enrollment becomes Revoked and the completion event carries no actor (AT-071), because
/// the last confirmation is a device callback, not a person's decision.
/// </summary>
public sealed class BiometricEnrollment : AggregateRoot<BiometricEnrollmentId>
{
    public Guid TenantId { get; }

    public Guid EmployeeId { get; }

    public BiometricMethod Method { get; }

    public BiometricTemplateReference TemplateReference { get; }

    public EnrollmentStatus Status { get; private set; }

    public string? Vendor { get; private set; }

    private readonly List<Guid> _synchronizedDeviceIds = new();

    public IReadOnlyList<Guid> SynchronizedDeviceIds => _synchronizedDeviceIds.AsReadOnly();

    private readonly List<Guid> _pendingRevocationDeviceIds = new();

    public IReadOnlyList<Guid> PendingRevocationDeviceIds => _pendingRevocationDeviceIds.AsReadOnly();

    private BiometricEnrollment(
        BiometricEnrollmentId id, Guid tenantId, Guid employeeId, BiometricMethod method,
        BiometricTemplateReference templateReference, string? vendor)
        : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        Method = method;
        TemplateReference = templateReference;
        Vendor = vendor;
        Status = EnrollmentStatus.Enrolled;
    }

    public static Result<BiometricEnrollment> Enroll(
        BiometricEnrollmentId id, Guid tenantId, Guid employeeId, BiometricMethod method, string? templatePointer,
        bool consentConfirmed, string? vendor, Guid actorId, DateTimeOffset createdOnUtc)
    {
        if (!consentConfirmed)
        {
            return Result.Failure<BiometricEnrollment>(AttendanceErrors.BiometricConsentRequired);
        }

        if (employeeId == Guid.Empty)
        {
            return Result.Failure<BiometricEnrollment>(AttendanceErrors.EmployeeIdentifierRequired);
        }

        var referenceResult = BiometricTemplateReference.Create(templatePointer);
        if (referenceResult.IsFailure)
        {
            return Result.Failure<BiometricEnrollment>(referenceResult.Error);
        }

        var enrollment = new BiometricEnrollment(id, tenantId, employeeId, method, referenceResult.Value, vendor);
        enrollment.AddDomainEvent(new BiometricEnrollmentCreated(
            Guid.NewGuid(), createdOnUtc, id, tenantId, employeeId, method, actorId));
        return Result.Success(enrollment);
    }

    /// <summary>True only when this enrollment may be used for authentication (AT-052/AT-053).</summary>
    public bool IsActive() => Status == EnrollmentStatus.Active;

    /// <summary>Confirms device synchronization completed; moves the enrollment to Active.</summary>
    public Result Activate(Guid actorId, DateTimeOffset nowUtc)
    {
        if (Status == EnrollmentStatus.Active)
        {
            return Result.Success();
        }

        if (Status == EnrollmentStatus.Revoked)
        {
            return Result.Failure(AttendanceErrors.BiometricEnrollmentNotActive);
        }

        Status = EnrollmentStatus.Active;
        AddDomainEvent(new BiometricEnrollmentActivated(Guid.NewGuid(), nowUtc, Id, TenantId, actorId));
        return Result.Success();
    }

    /// <summary>
    /// Begins revocation. If no device holds a synchronized copy, revocation completes
    /// immediately with the requesting actor; otherwise the synchronized set is moved to
    /// the pending-revocation set and completion waits on device callbacks (AT-053).
    /// </summary>
    public Result Revoke(Guid actorId, string reason, DateTimeOffset nowUtc)
    {
        if (Status == EnrollmentStatus.Revoked)
        {
            return Result.Success();
        }

        if (Status != EnrollmentStatus.Active && Status != EnrollmentStatus.Enrolled)
        {
            return Result.Failure(AttendanceErrors.BiometricEnrollmentNotActive);
        }

        _pendingRevocationDeviceIds.AddRange(_synchronizedDeviceIds);
        _synchronizedDeviceIds.Clear();

        if (_pendingRevocationDeviceIds.Count == 0)
        {
            Status = EnrollmentStatus.Revoked;
            AddDomainEvent(new BiometricEnrollmentRevoked(Guid.NewGuid(), nowUtc, Id, TenantId, actorId, reason));
        }

        return Result.Success();
    }

    /// <summary>Device callback confirming removal of the synchronized template (AT-053).</summary>
    public Result ConfirmDeviceRevocation(Guid deviceId, DateTimeOffset nowUtc)
    {
        if (Status == EnrollmentStatus.Revoked)
        {
            return Result.Success();
        }

        _pendingRevocationDeviceIds.Remove(deviceId);

        if (_pendingRevocationDeviceIds.Count == 0)
        {
            Status = EnrollmentStatus.Revoked;
            AddDomainEvent(new BiometricEnrollmentRevoked(Guid.NewGuid(), nowUtc, Id, TenantId, null, "All synchronized devices confirmed revocation."));
        }

        return Result.Success();
    }

    /// <summary>Records that a device now holds a synchronized copy of the template.</summary>
    public Result AddSynchronizedDevice(Guid deviceId)
    {
        if (Status == EnrollmentStatus.Revoked)
        {
            return Result.Failure(AttendanceErrors.BiometricEnrollmentNotActive);
        }

        if (!_synchronizedDeviceIds.Contains(deviceId))
        {
            _synchronizedDeviceIds.Add(deviceId);
        }

        return Result.Success();
    }
}
