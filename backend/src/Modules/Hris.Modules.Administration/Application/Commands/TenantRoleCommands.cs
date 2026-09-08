using Hris.Application.Abstractions;
using Hris.Modules.Administration.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Administration.Application.Commands;

public sealed record DefineTenantRoleCommand(
    Guid TenantId, string? Name, string? Description, Guid CreatedBy, IReadOnlyList<string?>? InitialPermissions) : ICommand<Result<Guid>>;

internal sealed class DefineTenantRoleCommandHandler : IRequestHandler<DefineTenantRoleCommand, Result<Guid>>
{
    private readonly ITenantRoleRepository _repository;
    private readonly TimeProvider _timeProvider;

    public DefineTenantRoleCommandHandler(ITenantRoleRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(DefineTenantRoleCommand request, CancellationToken cancellationToken)
    {
        var collidesWithCanonical = !string.IsNullOrWhiteSpace(request.Name)
            && Enum.GetNames<CanonicalRole>().Any(name => string.Equals(name, request.Name.Trim(), StringComparison.OrdinalIgnoreCase));

        var alreadyExists = !string.IsNullOrWhiteSpace(request.Name)
            && await _repository.ExistsWithNameAsync(request.TenantId, request.Name, null, cancellationToken).ConfigureAwait(false);

        var id = new TenantRoleId(Guid.NewGuid());
        var nowUtc = _timeProvider.GetUtcNow();

        var roleResult = TenantRole.Create(
            id, request.TenantId, request.Name, request.Description, collidesWithCanonical, alreadyExists, request.CreatedBy, nowUtc);
        if (roleResult.IsFailure)
        {
            return Result.Failure<Guid>(roleResult.Error);
        }

        var role = roleResult.Value;

        if (request.InitialPermissions is { Count: > 0 })
        {
            foreach (var permission in request.InitialPermissions)
            {
                var addResult = role.AddPermission(permission, request.CreatedBy, nowUtc);
                if (addResult.IsFailure)
                {
                    return Result.Failure<Guid>(addResult.Error);
                }
            }
        }

        await _repository.AddAsync(role, cancellationToken).ConfigureAwait(false);
        return Result.Success(role.Id.Value);
    }
}

public sealed record AddPermissionToTenantRoleCommand(Guid TenantRoleId, Guid TenantId, string? Permission, Guid AddedBy)
    : ICommand<Result>;

internal sealed class AddPermissionToTenantRoleCommandHandler : IRequestHandler<AddPermissionToTenantRoleCommand, Result>
{
    private readonly ITenantRoleRepository _repository;
    private readonly TimeProvider _timeProvider;

    public AddPermissionToTenantRoleCommandHandler(ITenantRoleRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(AddPermissionToTenantRoleCommand request, CancellationToken cancellationToken)
    {
        var result = await AdministrationLookup.LoadTenantRoleForTenantAsync(
            _repository, request.TenantRoleId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return result.IsFailure
            ? Result.Failure(result.Error)
            : result.Value.AddPermission(request.Permission, request.AddedBy, _timeProvider.GetUtcNow());
    }
}

public sealed record RemovePermissionFromTenantRoleCommand(Guid TenantRoleId, Guid TenantId, Guid PermissionGrantId) : ICommand<Result>;

internal sealed class RemovePermissionFromTenantRoleCommandHandler : IRequestHandler<RemovePermissionFromTenantRoleCommand, Result>
{
    private readonly ITenantRoleRepository _repository;

    public RemovePermissionFromTenantRoleCommandHandler(ITenantRoleRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result> Handle(RemovePermissionFromTenantRoleCommand request, CancellationToken cancellationToken)
    {
        var result = await AdministrationLookup.LoadTenantRoleForTenantAsync(
            _repository, request.TenantRoleId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return result.IsFailure ? Result.Failure(result.Error) : result.Value.RemovePermission(request.PermissionGrantId);
    }
}

public sealed record PublishTenantRoleCommand(Guid TenantRoleId, Guid TenantId, Guid PublishedBy) : ICommand<Result>;

internal sealed class PublishTenantRoleCommandHandler : IRequestHandler<PublishTenantRoleCommand, Result>
{
    private readonly ITenantRoleRepository _repository;
    private readonly TimeProvider _timeProvider;

    public PublishTenantRoleCommandHandler(ITenantRoleRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(PublishTenantRoleCommand request, CancellationToken cancellationToken)
    {
        var result = await AdministrationLookup.LoadTenantRoleForTenantAsync(
            _repository, request.TenantRoleId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return result.IsFailure ? Result.Failure(result.Error) : result.Value.Publish(request.PublishedBy, _timeProvider.GetUtcNow());
    }
}

/// <summary>
/// Published-role permission changes are a separate command from
/// <see cref="AddPermissionToTenantRoleCommand"/>: this one requires a reason and
/// silently changes the authority of every current holder (commands.md's own
/// "Draft and Published Changes Are Different Commands").
/// </summary>
public sealed record ChangeTenantRolePermissionsCommand(
    Guid TenantRoleId,
    Guid TenantId,
    IReadOnlyList<string?> PermissionsToAdd,
    IReadOnlyList<Guid> PermissionGrantIdsToRemove,
    Guid ChangedBy,
    string? Reason) : ICommand<Result>;

internal sealed class ChangeTenantRolePermissionsCommandHandler : IRequestHandler<ChangeTenantRolePermissionsCommand, Result>
{
    private readonly ITenantRoleRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ChangeTenantRolePermissionsCommandHandler(ITenantRoleRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ChangeTenantRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        var result = await AdministrationLookup.LoadTenantRoleForTenantAsync(
            _repository, request.TenantRoleId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return result.IsFailure
            ? Result.Failure(result.Error)
            : result.Value.ChangePermissions(
                request.PermissionsToAdd, request.PermissionGrantIdsToRemove, request.ChangedBy, request.Reason,
                _timeProvider.GetUtcNow());
    }
}

public sealed record DeprecateTenantRoleCommand(Guid TenantRoleId, Guid TenantId) : ICommand<Result>;

internal sealed class DeprecateTenantRoleCommandHandler : IRequestHandler<DeprecateTenantRoleCommand, Result>
{
    private readonly ITenantRoleRepository _repository;
    private readonly TimeProvider _timeProvider;

    public DeprecateTenantRoleCommandHandler(ITenantRoleRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(DeprecateTenantRoleCommand request, CancellationToken cancellationToken)
    {
        var result = await AdministrationLookup.LoadTenantRoleForTenantAsync(
            _repository, request.TenantRoleId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return result.IsFailure ? Result.Failure(result.Error) : result.Value.Deprecate(_timeProvider.GetUtcNow());
    }
}

/// <summary>AR-032: rejected if any active assignment holds this role.</summary>
public sealed record DeleteTenantRoleCommand(Guid TenantRoleId, Guid TenantId) : ICommand<Result>;

internal sealed class DeleteTenantRoleCommandHandler : IRequestHandler<DeleteTenantRoleCommand, Result>
{
    private readonly ITenantRoleRepository _repository;
    private readonly IUserAccountRepository _userAccountRepository;

    public DeleteTenantRoleCommandHandler(ITenantRoleRepository repository, IUserAccountRepository userAccountRepository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _userAccountRepository = Guard.AgainstNull(userAccountRepository, nameof(userAccountRepository));
    }

    public async Task<Result> Handle(DeleteTenantRoleCommand request, CancellationToken cancellationToken)
    {
        var result = await AdministrationLookup.LoadTenantRoleForTenantAsync(
            _repository, request.TenantRoleId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return Result.Failure(result.Error);
        }

        var isReferenced = await _userAccountRepository
            .HasActiveAssignmentForTenantRoleAsync(request.TenantId, request.TenantRoleId, cancellationToken)
            .ConfigureAwait(false);

        var deletableResult = result.Value.EnsureDeletable(isReferenced);
        if (deletableResult.IsFailure)
        {
            return deletableResult;
        }

        _repository.Remove(result.Value);
        return Result.Success();
    }
}
