using Hris.Application.Abstractions;
using Hris.Modules.Leave.Application.Dtos;
using Hris.Modules.Leave.Application.Mapping;
using Hris.Modules.Leave.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Leave.Application.Queries;

/// <summary>All active leave types, platform and tenant. Source: application/queries.md.</summary>
public sealed record GetLeaveTypeCatalogQuery(Guid TenantId) : IQuery<Result<IReadOnlyList<LeaveTypeDto>>>;

internal sealed class GetLeaveTypeCatalogQueryHandler
    : IRequestHandler<GetLeaveTypeCatalogQuery, Result<IReadOnlyList<LeaveTypeDto>>>
{
    private readonly ILeaveTypeRepository _repository;

    public GetLeaveTypeCatalogQueryHandler(ILeaveTypeRepository repository) =>
        _repository = Guard.AgainstNull(repository, nameof(repository));

    public async Task<Result<IReadOnlyList<LeaveTypeDto>>> Handle(
        GetLeaveTypeCatalogQuery request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var types = await _repository.ListCatalogForTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        var catalog = types
            .Where(type => type.Status == LeaveTypeStatus.Active)
            .OrderBy(type => type.Scope)
            .ThenBy(type => type.Name, StringComparer.Ordinal)
            .Select(LeaveMapper.ToDto)
            .ToList();

        return Result.Success((IReadOnlyList<LeaveTypeDto>)catalog);
    }
}
