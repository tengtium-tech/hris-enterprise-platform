using Hris.Application.Abstractions;
using Hris.Foundation.Authorization.Application.Dtos;
using Hris.Foundation.Authorization.Application.Mapping;
using Hris.Foundation.Authorization.Domain;
using Hris.Foundation.Identity.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Authorization.Application.Queries;

/// <summary>
/// Every role assignment a principal holds -- effective, expired, or revoked, per
/// <see cref="IRoleAssignmentRepository.GetByPrincipalAsync"/>'s own remarks -- for an
/// administrative "what does this person have" view. Not filtered to only-effective
/// here: an administrator reviewing a principal's access history needs to see
/// revoked/expired grants too, unlike <see cref="CheckAuthorizationQuery"/>'s own
/// evaluation, which must only ever consider what is currently in force.
/// </summary>
public sealed record GetRoleAssignmentsForPrincipalQuery(Guid PrincipalId) : IQuery<Result<IReadOnlyList<RoleAssignmentDto>>>;

internal sealed class GetRoleAssignmentsForPrincipalQueryHandler
    : IRequestHandler<GetRoleAssignmentsForPrincipalQuery, Result<IReadOnlyList<RoleAssignmentDto>>>
{
    private readonly IRoleAssignmentRepository _repository;

    public GetRoleAssignmentsForPrincipalQueryHandler(IRoleAssignmentRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<RoleAssignmentDto>>> Handle(
        GetRoleAssignmentsForPrincipalQuery request, CancellationToken cancellationToken)
    {
        var assignments = await _repository
            .GetByPrincipalAsync(new UserAccountId(request.PrincipalId), cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<RoleAssignmentDto> dtos = assignments.Select(a => a.ToDto()).ToList();

        return Result.Success(dtos);
    }
}
