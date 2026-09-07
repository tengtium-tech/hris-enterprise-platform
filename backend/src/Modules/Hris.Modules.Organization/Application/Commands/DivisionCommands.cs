using Hris.Application.Abstractions;
using Hris.Modules.Organization.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Organization.Application.Commands;

/// <summary>
/// Division management commands. See <c>BusinessUnitCommands</c>'s own remarks for
/// the shared load-then-delegate shape every handler in this module follows.
/// </summary>
public sealed record CreateDivisionCommand(
    Guid OrganizationId, Guid TenantId, Guid BusinessUnitId, string? Name) : ICommand<Result<Guid>>;

internal sealed class CreateDivisionCommandHandler : IRequestHandler<CreateDivisionCommand, Result<Guid>>
{
    private readonly IOrganizationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CreateDivisionCommandHandler(IOrganizationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(CreateDivisionCommand request, CancellationToken cancellationToken)
    {
        var organizationResult = await OrganizationLookup.LoadOrganizationForTenantAsync(
            _repository, request.OrganizationId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (organizationResult.IsFailure)
        {
            return Result.Failure<Guid>(organizationResult.Error);
        }

        var result = organizationResult.Value.AddDivision(
            new BusinessUnitId(request.BusinessUnitId), request.Name, _timeProvider.GetUtcNow());
        return result.IsFailure ? Result.Failure<Guid>(result.Error) : Result.Success(result.Value.Value);
    }
}

public sealed record RenameDivisionCommand(Guid OrganizationId, Guid TenantId, Guid DivisionId, string? NewName) : ICommand<Result>;

internal sealed class RenameDivisionCommandHandler : IRequestHandler<RenameDivisionCommand, Result>
{
    private readonly IOrganizationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RenameDivisionCommandHandler(IOrganizationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RenameDivisionCommand request, CancellationToken cancellationToken)
    {
        var organizationResult = await OrganizationLookup.LoadOrganizationForTenantAsync(
            _repository, request.OrganizationId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return organizationResult.IsFailure
            ? Result.Failure(organizationResult.Error)
            : organizationResult.Value.RenameDivision(new DivisionId(request.DivisionId), request.NewName, _timeProvider.GetUtcNow());
    }
}

public sealed record MoveDivisionCommand(
    Guid OrganizationId, Guid TenantId, Guid DivisionId, Guid NewBusinessUnitId) : ICommand<Result>;

internal sealed class MoveDivisionCommandHandler : IRequestHandler<MoveDivisionCommand, Result>
{
    private readonly IOrganizationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public MoveDivisionCommandHandler(IOrganizationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(MoveDivisionCommand request, CancellationToken cancellationToken)
    {
        var organizationResult = await OrganizationLookup.LoadOrganizationForTenantAsync(
            _repository, request.OrganizationId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return organizationResult.IsFailure
            ? Result.Failure(organizationResult.Error)
            : organizationResult.Value.MoveDivision(
                new DivisionId(request.DivisionId), new BusinessUnitId(request.NewBusinessUnitId), _timeProvider.GetUtcNow());
    }
}

public sealed record ArchiveDivisionCommand(Guid OrganizationId, Guid TenantId, Guid DivisionId) : ICommand<Result>;

internal sealed class ArchiveDivisionCommandHandler : IRequestHandler<ArchiveDivisionCommand, Result>
{
    private readonly IOrganizationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ArchiveDivisionCommandHandler(IOrganizationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ArchiveDivisionCommand request, CancellationToken cancellationToken)
    {
        var organizationResult = await OrganizationLookup.LoadOrganizationForTenantAsync(
            _repository, request.OrganizationId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return organizationResult.IsFailure
            ? Result.Failure(organizationResult.Error)
            : organizationResult.Value.ArchiveDivision(new DivisionId(request.DivisionId), _timeProvider.GetUtcNow());
    }
}
