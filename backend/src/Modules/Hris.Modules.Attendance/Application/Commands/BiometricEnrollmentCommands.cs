using Hris.Application.Abstractions;
using Hris.Modules.Attendance.Application;
using Hris.Modules.Attendance.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Attendance.Application.Commands;

/// <summary>
/// Enrolls a biometric registration. Consent confirmation is a required field, never implied
/// (../security/compliance.md, AT-052). Only an encrypted, non-reversible template reference is
/// stored — never raw biometric bytes.
/// </summary>
public sealed record EnrollBiometricCommand(
    Guid TenantId,
    Guid EmployeeId,
    BiometricMethod Method,
    string? TemplatePointer,
    bool ConsentConfirmed,
    string? Vendor,
    Guid ActorId) : ICommand<Result<Guid>>;

internal sealed class EnrollBiometricCommandHandler : IRequestHandler<EnrollBiometricCommand, Result<Guid>>
{
    private readonly IBiometricEnrollmentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public EnrollBiometricCommandHandler(IBiometricEnrollmentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(EnrollBiometricCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var enrollResult = BiometricEnrollment.Enroll(
            new BiometricEnrollmentId(Guid.NewGuid()),
            request.TenantId,
            request.EmployeeId,
            request.Method,
            request.TemplatePointer,
            request.ConsentConfirmed,
            request.Vendor,
            request.ActorId,
            _timeProvider.GetUtcNow());

        if (enrollResult.IsFailure)
        {
            return Result.Failure<Guid>(enrollResult.Error);
        }

        await _repository.AddAsync(enrollResult.Value, cancellationToken).ConfigureAwait(false);
        return Result.Success(enrollResult.Value.Id.Value);
    }
}

/// <summary>Confirms device synchronization completed; moves the enrollment to Active.</summary>
public sealed record ActivateBiometricEnrollmentCommand(Guid TenantId, Guid BiometricEnrollmentId, Guid ActorId)
    : ICommand<Result>;

internal sealed class ActivateBiometricEnrollmentCommandHandler
    : IRequestHandler<ActivateBiometricEnrollmentCommand, Result>
{
    private readonly IBiometricEnrollmentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ActivateBiometricEnrollmentCommandHandler(IBiometricEnrollmentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ActivateBiometricEnrollmentCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var enrollmentResult = await AttendanceLookup
            .LoadEnrollmentForTenantAsync(_repository, request.BiometricEnrollmentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (enrollmentResult.IsFailure)
        {
            return Result.Failure(enrollmentResult.Error);
        }

        return enrollmentResult.Value.Activate(request.ActorId, _timeProvider.GetUtcNow());
    }
}

/// <summary>
/// Begins revocation. Not complete until every synchronized device confirms removal (AT-053); the
/// completion event carries no actor because the last confirmation is a device callback (AT-071).
/// </summary>
public sealed record RevokeBiometricEnrollmentCommand(Guid TenantId, Guid BiometricEnrollmentId, Guid ActorId, string Reason)
    : ICommand<Result>;

internal sealed class RevokeBiometricEnrollmentCommandHandler : IRequestHandler<RevokeBiometricEnrollmentCommand, Result>
{
    private readonly IBiometricEnrollmentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RevokeBiometricEnrollmentCommandHandler(IBiometricEnrollmentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RevokeBiometricEnrollmentCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var enrollmentResult = await AttendanceLookup
            .LoadEnrollmentForTenantAsync(_repository, request.BiometricEnrollmentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (enrollmentResult.IsFailure)
        {
            return Result.Failure(enrollmentResult.Error);
        }

        return enrollmentResult.Value.Revoke(request.ActorId, request.Reason, _timeProvider.GetUtcNow());
    }
}
