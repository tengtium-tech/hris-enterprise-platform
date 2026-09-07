using Hris.Application.Abstractions;
using Hris.Modules.Organization.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Organization.Application.Commands;

/// <summary>
/// Cost Center management commands, including CC-001's own tenant-wide code
/// uniqueness check.
/// </summary>
public sealed record CreateCostCenterCommand(
    Guid OrganizationId, Guid TenantId, string? Code, string? Description) : ICommand<Result<Guid>>;

internal sealed class CreateCostCenterCommandHandler : IRequestHandler<CreateCostCenterCommand, Result<Guid>>
{
    private readonly IOrganizationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CreateCostCenterCommandHandler(IOrganizationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(CreateCostCenterCommand request, CancellationToken cancellationToken)
    {
        if (request.Code is not null
            && await _repository.ExistsCostCenterWithCodeAsync(request.TenantId, request.Code, null, cancellationToken)
                .ConfigureAwait(false))
        {
            return Result.Failure<Guid>(OrganizationErrors.DuplicateCostCenterCode);
        }

        var organizationResult = await OrganizationLookup.LoadOrganizationForTenantAsync(
            _repository, request.OrganizationId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (organizationResult.IsFailure)
        {
            return Result.Failure<Guid>(organizationResult.Error);
        }

        var result = organizationResult.Value.AddCostCenter(request.Code, request.Description, _timeProvider.GetUtcNow());
        return result.IsFailure ? Result.Failure<Guid>(result.Error) : Result.Success(result.Value.Value);
    }
}

public sealed record UpdateCostCenterCommand(
    Guid OrganizationId, Guid TenantId, Guid CostCenterId, string? Description) : ICommand<Result>;

internal sealed class UpdateCostCenterCommandHandler : IRequestHandler<UpdateCostCenterCommand, Result>
{
    private readonly IOrganizationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public UpdateCostCenterCommandHandler(IOrganizationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(UpdateCostCenterCommand request, CancellationToken cancellationToken)
    {
        var organizationResult = await OrganizationLookup.LoadOrganizationForTenantAsync(
            _repository, request.OrganizationId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return organizationResult.IsFailure
            ? Result.Failure(organizationResult.Error)
            : organizationResult.Value.UpdateCostCenter(
                new CostCenterId(request.CostCenterId), request.Description, _timeProvider.GetUtcNow());
    }
}

public sealed record ArchiveCostCenterCommand(Guid OrganizationId, Guid TenantId, Guid CostCenterId) : ICommand<Result>;

internal sealed class ArchiveCostCenterCommandHandler : IRequestHandler<ArchiveCostCenterCommand, Result>
{
    private readonly IOrganizationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ArchiveCostCenterCommandHandler(IOrganizationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ArchiveCostCenterCommand request, CancellationToken cancellationToken)
    {
        var organizationResult = await OrganizationLookup.LoadOrganizationForTenantAsync(
            _repository, request.OrganizationId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return organizationResult.IsFailure
            ? Result.Failure(organizationResult.Error)
            : organizationResult.Value.ArchiveCostCenter(new CostCenterId(request.CostCenterId), _timeProvider.GetUtcNow());
    }
}
