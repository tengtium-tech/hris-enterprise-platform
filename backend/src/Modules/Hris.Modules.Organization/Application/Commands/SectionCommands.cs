using Hris.Application.Abstractions;
using Hris.Modules.Organization.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Organization.Application.Commands;

/// <summary>
/// Section management commands. See <c>BusinessUnitCommands</c>'s own remarks for
/// the shared load-then-delegate shape every handler in this module follows.
/// </summary>
public sealed record CreateSectionCommand(Guid OrganizationId, Guid TenantId, Guid DepartmentId, string? Name)
    : ICommand<Result<Guid>>;

internal sealed class CreateSectionCommandHandler : IRequestHandler<CreateSectionCommand, Result<Guid>>
{
    private readonly IOrganizationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CreateSectionCommandHandler(IOrganizationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(CreateSectionCommand request, CancellationToken cancellationToken)
    {
        var organizationResult = await OrganizationLookup.LoadOrganizationForTenantAsync(
            _repository, request.OrganizationId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (organizationResult.IsFailure)
        {
            return Result.Failure<Guid>(organizationResult.Error);
        }

        var result = organizationResult.Value.AddSection(
            new DepartmentId(request.DepartmentId), request.Name, _timeProvider.GetUtcNow());
        return result.IsFailure ? Result.Failure<Guid>(result.Error) : Result.Success(result.Value.Value);
    }
}

public sealed record RenameSectionCommand(Guid OrganizationId, Guid TenantId, Guid SectionId, string? NewName) : ICommand<Result>;

internal sealed class RenameSectionCommandHandler : IRequestHandler<RenameSectionCommand, Result>
{
    private readonly IOrganizationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RenameSectionCommandHandler(IOrganizationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RenameSectionCommand request, CancellationToken cancellationToken)
    {
        var organizationResult = await OrganizationLookup.LoadOrganizationForTenantAsync(
            _repository, request.OrganizationId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return organizationResult.IsFailure
            ? Result.Failure(organizationResult.Error)
            : organizationResult.Value.RenameSection(new SectionId(request.SectionId), request.NewName, _timeProvider.GetUtcNow());
    }
}

public sealed record ArchiveSectionCommand(Guid OrganizationId, Guid TenantId, Guid SectionId) : ICommand<Result>;

internal sealed class ArchiveSectionCommandHandler : IRequestHandler<ArchiveSectionCommand, Result>
{
    private readonly IOrganizationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ArchiveSectionCommandHandler(IOrganizationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ArchiveSectionCommand request, CancellationToken cancellationToken)
    {
        var organizationResult = await OrganizationLookup.LoadOrganizationForTenantAsync(
            _repository, request.OrganizationId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return organizationResult.IsFailure
            ? Result.Failure(organizationResult.Error)
            : organizationResult.Value.ArchiveSection(new SectionId(request.SectionId), _timeProvider.GetUtcNow());
    }
}
