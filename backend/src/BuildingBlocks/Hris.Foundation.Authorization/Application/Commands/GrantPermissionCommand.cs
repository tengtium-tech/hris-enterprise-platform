using Hris.Application.Abstractions;
using Hris.Foundation.Authorization.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Authorization.Application.Commands;

/// <summary>
/// Permission Management: grants one <see cref="PermissionAction"/> on one resource
/// type to one <see cref="Role"/>. `CTR-AUT-003` ("Auditor Holds No Mutation
/// Permissions") is enforced inside <see cref="RolePermissionGrant.Create"/> itself,
/// not re-checked here -- the identical separation every other command handler this
/// Sprint keeps between validation and business-rule enforcement.
/// </summary>
public sealed record GrantPermissionCommand(Role Role, string ResourceType, PermissionAction Action) : ICommand<Result<Guid>>;

internal sealed class GrantPermissionCommandHandler : IRequestHandler<GrantPermissionCommand, Result<Guid>>
{
    private readonly IRolePermissionGrantRepository _repository;
    private readonly TimeProvider _timeProvider;

    public GrantPermissionCommandHandler(IRolePermissionGrantRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(GrantPermissionCommand request, CancellationToken cancellationToken)
    {
        var permissionResult = PermissionKey.Create(request.ResourceType, request.Action);
        if (permissionResult.IsFailure)
        {
            return Result.Failure<Guid>(permissionResult.Error);
        }

        var grantResult = RolePermissionGrant.Create(request.Role, permissionResult.Value, _timeProvider.GetUtcNow());
        if (grantResult.IsFailure)
        {
            return Result.Failure<Guid>(grantResult.Error);
        }

        await _repository.AddAsync(grantResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(grantResult.Value.Id.Value);
    }
}
