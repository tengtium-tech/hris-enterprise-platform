using Hris.Application.Abstractions;
using Hris.Foundation.Authorization.Application.Commands;
using Hris.Foundation.Authorization.Application.Dtos;
using Hris.Foundation.Authorization.Application.Mapping;
using Hris.Foundation.Authorization.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Authorization.Application.Queries;

/// <summary>
/// Every active permission grant held by one <see cref="Role"/> -- the Role
/// Management/Permission Management "what can this role do" view, and the
/// counterpart read to <see cref="GrantPermissionCommand"/>/<see cref="RevokePermissionCommand"/>.
/// </summary>
public sealed record GetActivePermissionsForRoleQuery(Role Role) : IQuery<Result<IReadOnlyList<PermissionGrantDto>>>;

internal sealed class GetActivePermissionsForRoleQueryHandler
    : IRequestHandler<GetActivePermissionsForRoleQuery, Result<IReadOnlyList<PermissionGrantDto>>>
{
    private readonly IRolePermissionGrantRepository _repository;

    public GetActivePermissionsForRoleQueryHandler(IRolePermissionGrantRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<PermissionGrantDto>>> Handle(
        GetActivePermissionsForRoleQuery request, CancellationToken cancellationToken)
    {
        var grants = await _repository
            .GetActiveGrantsForRolesAsync([request.Role], cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<PermissionGrantDto> dtos = grants.Select(g => g.ToDto()).ToList();

        return Result.Success(dtos);
    }
}
