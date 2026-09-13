using Hris.Application.Abstractions;
using Hris.Modules.Attendance.Application;
using Hris.Modules.Attendance.Application.Dtos;
using Hris.Modules.Attendance.Application.Mapping;
using Hris.Modules.Attendance.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Attendance.Application.Queries;

/// <summary>AttendanceDevice read surface (queries.md): device detail and health status.</summary>
public sealed record GetAttendanceDeviceQuery(Guid TenantId, Guid AttendanceDeviceId)
    : IQuery<Result<AttendanceDeviceDto>>;

internal sealed class GetAttendanceDeviceQueryHandler
    : IRequestHandler<GetAttendanceDeviceQuery, Result<AttendanceDeviceDto>>
{
    private readonly IAttendanceDeviceRepository _repository;

    public GetAttendanceDeviceQueryHandler(IAttendanceDeviceRepository repository) =>
        _repository = Guard.AgainstNull(repository, nameof(repository));

    public async Task<Result<AttendanceDeviceDto>> Handle(
        GetAttendanceDeviceQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var result = await AttendanceLookup
            .LoadDeviceForTenantAsync(_repository, request.AttendanceDeviceId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return result.IsFailure
            ? Result.Failure<AttendanceDeviceDto>(result.Error)
            : Result.Success(AttendanceMapper.ToDto(result.Value));
    }
}
