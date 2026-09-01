using Hris.Application.Abstractions;
using Hris.Foundation.Authorization.Domain;
using Hris.Foundation.Identity.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Authorization.Application.Commands;

/// <summary>
/// Role Management / Delegated Administration: grants one <see cref="Role"/> to one
/// principal at one <see cref="OrganizationalScope"/>. Delegated administration is
/// this same command with a restricted <see cref="ScopeLevel"/>/<see cref="ScopeId"/>,
/// per authorization-framework.md's own "Delegation is expressed as role plus scope,
/// never as a new role" -- there is no separate "delegate administration" command.
/// </summary>
public sealed record AssignRoleCommand(
    Guid PrincipalId,
    Role Role,
    OrganizationalScopeLevel ScopeLevel,
    Guid ScopeId,
    RoleAssignmentType AssignmentType,
    DateOnly EffectiveDate,
    DateOnly? ExpirationDate,
    Guid GrantedByPrincipalId) : ICommand<Result<Guid>>;

internal sealed class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand, Result<Guid>>
{
    private readonly IRoleAssignmentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public AssignRoleCommandHandler(IRoleAssignmentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var scopeResult = OrganizationalScope.Create(request.ScopeLevel, request.ScopeId);
        if (scopeResult.IsFailure)
        {
            return Result.Failure<Guid>(scopeResult.Error);
        }

        var assignmentResult = RoleAssignment.Create(
            new UserAccountId(request.PrincipalId),
            request.Role,
            scopeResult.Value,
            request.AssignmentType,
            request.EffectiveDate,
            request.ExpirationDate,
            new UserAccountId(request.GrantedByPrincipalId),
            _timeProvider.GetUtcNow());

        if (assignmentResult.IsFailure)
        {
            return Result.Failure<Guid>(assignmentResult.Error);
        }

        await _repository.AddAsync(assignmentResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(assignmentResult.Value.Id.Value);
    }
}
