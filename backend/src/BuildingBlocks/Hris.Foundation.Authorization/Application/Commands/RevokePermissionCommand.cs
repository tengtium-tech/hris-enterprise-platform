using Hris.Application.Abstractions;
using Hris.Foundation.Authorization.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Authorization.Application.Commands;

/// <summary>Revokes one <see cref="RolePermissionGrant"/>.</summary>
public sealed record RevokePermissionCommand(Guid RolePermissionGrantId) : ICommand<Result>;

internal sealed class RevokePermissionCommandHandler : IRequestHandler<RevokePermissionCommand, Result>
{
    private readonly IRolePermissionGrantRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RevokePermissionCommandHandler(IRolePermissionGrantRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RevokePermissionCommand request, CancellationToken cancellationToken)
    {
        var grant = await _repository
            .GetByIdAsync(new RolePermissionGrantId(request.RolePermissionGrantId), cancellationToken)
            .ConfigureAwait(false);

        return grant is null
            ? Result.Failure(AuthorizationErrors.RolePermissionGrantNotFound)
            : grant.Revoke(_timeProvider.GetUtcNow());
    }
}
