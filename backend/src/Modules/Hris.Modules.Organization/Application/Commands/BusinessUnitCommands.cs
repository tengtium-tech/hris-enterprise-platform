using Hris.Application.Abstractions;
using Hris.Modules.Organization.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Organization.Application.Commands;

/// <summary>
/// Business Unit management commands. Every handler here loads the owning
/// <see cref="Domain.Organization"/> (tenant-checked via <see cref="OrganizationLookup"/>)
/// and delegates to the single corresponding Aggregate method -- BU-001's own
/// "unique within an Organization" name uniqueness is enforced inside
/// <see cref="Domain.Organization.AddBusinessUnit"/> itself, since every sibling is
/// already loaded within the same Aggregate instance, unlike ORG-001/ORG-002's own
/// tenant-wide scope.
/// </summary>
public sealed record CreateBusinessUnitCommand(Guid OrganizationId, Guid TenantId, string? Name) : ICommand<Result<Guid>>;

internal sealed class CreateBusinessUnitCommandHandler : IRequestHandler<CreateBusinessUnitCommand, Result<Guid>>
{
    private readonly IOrganizationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CreateBusinessUnitCommandHandler(IOrganizationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(CreateBusinessUnitCommand request, CancellationToken cancellationToken)
    {
        var organizationResult = await OrganizationLookup.LoadOrganizationForTenantAsync(
            _repository, request.OrganizationId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (organizationResult.IsFailure)
        {
            return Result.Failure<Guid>(organizationResult.Error);
        }

        var result = organizationResult.Value.AddBusinessUnit(request.Name, _timeProvider.GetUtcNow());
        return result.IsFailure ? Result.Failure<Guid>(result.Error) : Result.Success(result.Value.Value);
    }
}

public sealed record RenameBusinessUnitCommand(
    Guid OrganizationId, Guid TenantId, Guid BusinessUnitId, string? NewName) : ICommand<Result>;

internal sealed class RenameBusinessUnitCommandHandler : IRequestHandler<RenameBusinessUnitCommand, Result>
{
    private readonly IOrganizationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RenameBusinessUnitCommandHandler(IOrganizationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RenameBusinessUnitCommand request, CancellationToken cancellationToken)
    {
        var organizationResult = await OrganizationLookup.LoadOrganizationForTenantAsync(
            _repository, request.OrganizationId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return organizationResult.IsFailure
            ? Result.Failure(organizationResult.Error)
            : organizationResult.Value.RenameBusinessUnit(
                new BusinessUnitId(request.BusinessUnitId), request.NewName, _timeProvider.GetUtcNow());
    }
}

public sealed record ArchiveBusinessUnitCommand(Guid OrganizationId, Guid TenantId, Guid BusinessUnitId) : ICommand<Result>;

internal sealed class ArchiveBusinessUnitCommandHandler : IRequestHandler<ArchiveBusinessUnitCommand, Result>
{
    private readonly IOrganizationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ArchiveBusinessUnitCommandHandler(IOrganizationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ArchiveBusinessUnitCommand request, CancellationToken cancellationToken)
    {
        var organizationResult = await OrganizationLookup.LoadOrganizationForTenantAsync(
            _repository, request.OrganizationId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return organizationResult.IsFailure
            ? Result.Failure(organizationResult.Error)
            : organizationResult.Value.ArchiveBusinessUnit(new BusinessUnitId(request.BusinessUnitId), _timeProvider.GetUtcNow());
    }
}

public sealed record RestoreBusinessUnitCommand(Guid OrganizationId, Guid TenantId, Guid BusinessUnitId) : ICommand<Result>;

internal sealed class RestoreBusinessUnitCommandHandler : IRequestHandler<RestoreBusinessUnitCommand, Result>
{
    private readonly IOrganizationRepository _repository;

    public RestoreBusinessUnitCommandHandler(IOrganizationRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result> Handle(RestoreBusinessUnitCommand request, CancellationToken cancellationToken)
    {
        var organizationResult = await OrganizationLookup.LoadOrganizationForTenantAsync(
            _repository, request.OrganizationId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return organizationResult.IsFailure
            ? Result.Failure(organizationResult.Error)
            : organizationResult.Value.RestoreBusinessUnit(new BusinessUnitId(request.BusinessUnitId));
    }
}
