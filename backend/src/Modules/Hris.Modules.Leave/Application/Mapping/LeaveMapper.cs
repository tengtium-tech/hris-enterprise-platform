using Hris.Modules.Leave.Application.Dtos;
using Hris.Modules.Leave.Domain;

namespace Hris.Modules.Leave.Application.Mapping;

/// <summary>Maps Leave aggregates to their DTOs (application/mapping.md). Grown as each aggregate is built.</summary>
public static class LeaveMapper
{
    public static LeaveTypeDto ToDto(LeaveType leaveType)
    {
        ArgumentNullException.ThrowIfNull(leaveType);

        return new LeaveTypeDto(
            leaveType.Id.Value,
            leaveType.TenantId,
            leaveType.Code,
            leaveType.Name,
            leaveType.Category.ToString(),
            leaveType.Scope.ToString(),
            leaveType.StatutoryBasis,
            leaveType.StatutoryMinimum,
            leaveType.Status.ToString());
    }
}
