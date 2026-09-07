using Hris.Application.Abstractions;
using Hris.Modules.Organization.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Organization.Application.Commands;

/// <summary>
/// Team management commands. See <c>BusinessUnitCommands</c>'s own remarks for the
/// shared load-then-delegate shape every handler in this module follows.
/// </summary>
public sealed record CreateTeamCommand(Guid OrganizationId, Guid TenantId, Guid SectionId, string? Name) : ICommand<Result<Guid>>;

internal sealed class CreateTeamCommandHandler : IRequestHandler<CreateTeamCommand, Result<Guid>>
{
    private readonly IOrganizationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CreateTeamCommandHandler(IOrganizationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(CreateTeamCommand request, CancellationToken cancellationToken)
    {
        var organizationResult = await OrganizationLookup.LoadOrganizationForTenantAsync(
            _repository, request.OrganizationId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (organizationResult.IsFailure)
        {
            return Result.Failure<Guid>(organizationResult.Error);
        }

        var result = organizationResult.Value.AddTeam(new SectionId(request.SectionId), request.Name, _timeProvider.GetUtcNow());
        return result.IsFailure ? Result.Failure<Guid>(result.Error) : Result.Success(result.Value.Value);
    }
}

public sealed record RenameTeamCommand(Guid OrganizationId, Guid TenantId, Guid TeamId, string? NewName) : ICommand<Result>;

internal sealed class RenameTeamCommandHandler : IRequestHandler<RenameTeamCommand, Result>
{
    private readonly IOrganizationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RenameTeamCommandHandler(IOrganizationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RenameTeamCommand request, CancellationToken cancellationToken)
    {
        var organizationResult = await OrganizationLookup.LoadOrganizationForTenantAsync(
            _repository, request.OrganizationId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return organizationResult.IsFailure
            ? Result.Failure(organizationResult.Error)
            : organizationResult.Value.RenameTeam(new TeamId(request.TeamId), request.NewName, _timeProvider.GetUtcNow());
    }
}

public sealed record ArchiveTeamCommand(Guid OrganizationId, Guid TenantId, Guid TeamId) : ICommand<Result>;

internal sealed class ArchiveTeamCommandHandler : IRequestHandler<ArchiveTeamCommand, Result>
{
    private readonly IOrganizationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ArchiveTeamCommandHandler(IOrganizationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ArchiveTeamCommand request, CancellationToken cancellationToken)
    {
        var organizationResult = await OrganizationLookup.LoadOrganizationForTenantAsync(
            _repository, request.OrganizationId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return organizationResult.IsFailure
            ? Result.Failure(organizationResult.Error)
            : organizationResult.Value.ArchiveTeam(new TeamId(request.TeamId), _timeProvider.GetUtcNow());
    }
}
