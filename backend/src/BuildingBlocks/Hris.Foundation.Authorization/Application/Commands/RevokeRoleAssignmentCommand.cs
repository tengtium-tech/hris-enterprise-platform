using Hris.Application.Abstractions;
using Hris.Foundation.Authorization.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Authorization.Application.Commands;

/// <summary>
/// `CTR-AUT-007` ("Revocation Takes Effect Immediately"): revokes one
/// <see cref="RoleAssignment"/>. Immediacy is a property of
/// <see cref="AuthorizationEvaluator"/> always reading a fresh
/// <see cref="IRoleAssignmentRepository"/> query rather than a cache -- this handler
/// only needs to persist the revocation; it does not need to invalidate anything
/// itself, per that evaluator's own remarks.
/// </summary>
public sealed record RevokeRoleAssignmentCommand(Guid RoleAssignmentId) : ICommand<Result>;

internal sealed class RevokeRoleAssignmentCommandHandler : IRequestHandler<RevokeRoleAssignmentCommand, Result>
{
    private readonly IRoleAssignmentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RevokeRoleAssignmentCommandHandler(IRoleAssignmentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RevokeRoleAssignmentCommand request, CancellationToken cancellationToken)
    {
        var assignment = await _repository
            .GetByIdAsync(new RoleAssignmentId(request.RoleAssignmentId), cancellationToken)
            .ConfigureAwait(false);

        return assignment is null
            ? Result.Failure(AuthorizationErrors.RoleAssignmentNotFound)
            : assignment.Revoke(_timeProvider.GetUtcNow());
    }
}
