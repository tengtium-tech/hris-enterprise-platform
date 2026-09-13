using Hris.Application.Abstractions;
using Hris.Modules.Attendance.Application;
using Hris.Modules.Attendance.Application.Dtos;
using Hris.Modules.Attendance.Application.Mapping;
using Hris.Modules.Attendance.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Attendance.Application.Queries;

/// <summary>
/// BiometricEnrollment read surface (queries.md). The status query is deliberately scoped to status
/// and method only — its DTO (BiometricEnrollmentStatusDto) carries no template field by construction,
/// so this handler cannot leak biometric template content even by mistake (dto-design.md).
/// </summary>
public sealed record GetBiometricEnrollmentStatusQuery(Guid TenantId, Guid BiometricEnrollmentId)
    : IQuery<Result<BiometricEnrollmentStatusDto>>;

internal sealed class GetBiometricEnrollmentStatusQueryHandler
    : IRequestHandler<GetBiometricEnrollmentStatusQuery, Result<BiometricEnrollmentStatusDto>>
{
    private readonly IBiometricEnrollmentRepository _repository;

    public GetBiometricEnrollmentStatusQueryHandler(IBiometricEnrollmentRepository repository) =>
        _repository = Guard.AgainstNull(repository, nameof(repository));

    public async Task<Result<BiometricEnrollmentStatusDto>> Handle(
        GetBiometricEnrollmentStatusQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var result = await AttendanceLookup
            .LoadEnrollmentForTenantAsync(_repository, request.BiometricEnrollmentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return result.IsFailure
            ? Result.Failure<BiometricEnrollmentStatusDto>(result.Error)
            : Result.Success(AttendanceMapper.ToDto(result.Value));
    }
}
