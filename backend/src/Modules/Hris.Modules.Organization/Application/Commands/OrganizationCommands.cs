using Hris.Application.Abstractions;
using Hris.Modules.Organization.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Organization.Application.Commands;

/// <summary>
/// Organization Aggregate's own lifecycle commands (Create, Update, Rename, Archive,
/// Restore), grouped into one file per this codebase's own established
/// "*LifecycleCommands" convention (see, for example,
/// <c>ConnectorLifecycleCommands</c>). ORG-001/ORG-002's own tenant-wide uniqueness
/// is checked here, in the Application layer, against <see cref="IOrganizationRepository"/>,
/// before <see cref="Organization.Create"/> or <see cref="Organization.Rename"/> is
/// called, since no single Aggregate instance can check uniqueness against its own
/// siblings.
/// </summary>
public sealed record CreateOrganizationCommand(
    Guid TenantId, string? Name, string? Code, Guid? LegalEntityId, string? Description) : ICommand<Result<Guid>>;

internal sealed class CreateOrganizationCommandHandler : IRequestHandler<CreateOrganizationCommand, Result<Guid>>
{
    private readonly IOrganizationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CreateOrganizationCommandHandler(IOrganizationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        if (request.Code is not null
            && await _repository.ExistsWithCodeAsync(request.TenantId, request.Code, null, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure<Guid>(OrganizationErrors.DuplicateOrganizationCode);
        }

        if (request.Name is not null
            && await _repository.ExistsWithNameAsync(request.TenantId, request.Name, null, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure<Guid>(OrganizationErrors.DuplicateOrganizationName);
        }

        var organizationResult = Domain.Organization.Create(
            new OrganizationId(Guid.NewGuid()), request.TenantId, request.Name, request.Code, request.LegalEntityId,
            request.Description, _timeProvider.GetUtcNow());

        if (organizationResult.IsFailure)
        {
            return Result.Failure<Guid>(organizationResult.Error);
        }

        await _repository.AddAsync(organizationResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(organizationResult.Value.Id.Value);
    }
}

public sealed record RenameOrganizationCommand(Guid OrganizationId, Guid TenantId, string? NewName) : ICommand<Result>;

internal sealed class RenameOrganizationCommandHandler : IRequestHandler<RenameOrganizationCommand, Result>
{
    private readonly IOrganizationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RenameOrganizationCommandHandler(IOrganizationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RenameOrganizationCommand request, CancellationToken cancellationToken)
    {
        if (request.NewName is not null
            && await _repository.ExistsWithNameAsync(request.TenantId, request.NewName, new OrganizationId(request.OrganizationId), cancellationToken)
                .ConfigureAwait(false))
        {
            return Result.Failure(OrganizationErrors.DuplicateOrganizationName);
        }

        var organizationResult = await OrganizationLookup.LoadOrganizationForTenantAsync(
            _repository, request.OrganizationId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return organizationResult.IsFailure
            ? Result.Failure(organizationResult.Error)
            : organizationResult.Value.Rename(request.NewName, _timeProvider.GetUtcNow());
    }
}

public sealed record UpdateOrganizationCommand(
    Guid OrganizationId, Guid TenantId, Guid? LegalEntityId, string? Description) : ICommand<Result>;

internal sealed class UpdateOrganizationCommandHandler : IRequestHandler<UpdateOrganizationCommand, Result>
{
    private readonly IOrganizationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public UpdateOrganizationCommandHandler(IOrganizationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(UpdateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organizationResult = await OrganizationLookup.LoadOrganizationForTenantAsync(
            _repository, request.OrganizationId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return organizationResult.IsFailure
            ? Result.Failure(organizationResult.Error)
            : organizationResult.Value.Update(request.LegalEntityId, request.Description, _timeProvider.GetUtcNow());
    }
}

public sealed record ArchiveOrganizationCommand(Guid OrganizationId, Guid TenantId) : ICommand<Result>;

internal sealed class ArchiveOrganizationCommandHandler : IRequestHandler<ArchiveOrganizationCommand, Result>
{
    private readonly IOrganizationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ArchiveOrganizationCommandHandler(IOrganizationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ArchiveOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organizationResult = await OrganizationLookup.LoadOrganizationForTenantAsync(
            _repository, request.OrganizationId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return organizationResult.IsFailure
            ? Result.Failure(organizationResult.Error)
            : organizationResult.Value.Archive(_timeProvider.GetUtcNow());
    }
}

public sealed record RestoreOrganizationCommand(Guid OrganizationId, Guid TenantId) : ICommand<Result>;

internal sealed class RestoreOrganizationCommandHandler : IRequestHandler<RestoreOrganizationCommand, Result>
{
    private readonly IOrganizationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RestoreOrganizationCommandHandler(IOrganizationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RestoreOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organizationResult = await OrganizationLookup.LoadOrganizationForTenantAsync(
            _repository, request.OrganizationId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return organizationResult.IsFailure
            ? Result.Failure(organizationResult.Error)
            : organizationResult.Value.Restore(_timeProvider.GetUtcNow());
    }
}
