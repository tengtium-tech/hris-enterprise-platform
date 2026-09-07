using Hris.Application.Abstractions;
using Hris.Modules.Organization.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Organization.Application.Commands;

/// <summary>
/// Department management commands, including DEPT-002's own tenant-wide code
/// uniqueness check (unlike DEPT-001's own narrower "within this Division" name
/// uniqueness, which <see cref="Domain.Organization.AddDepartment"/> already
/// enforces internally). <see cref="MergeDepartmentsCommand"/> and
/// <see cref="SplitDepartmentCommand"/> carry the identical
/// <see cref="IOrganizationRepository.ExistsDepartmentWithCodeAsync"/> check for
/// every new code they introduce.
/// </summary>
public sealed record CreateDepartmentCommand(
    Guid OrganizationId, Guid TenantId, Guid DivisionId, string? Name, string? Code) : ICommand<Result<Guid>>;

internal sealed class CreateDepartmentCommandHandler : IRequestHandler<CreateDepartmentCommand, Result<Guid>>
{
    private readonly IOrganizationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CreateDepartmentCommandHandler(IOrganizationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        if (request.Code is not null
            && await _repository.ExistsDepartmentWithCodeAsync(request.TenantId, request.Code, null, cancellationToken)
                .ConfigureAwait(false))
        {
            return Result.Failure<Guid>(OrganizationErrors.DuplicateDepartmentCode);
        }

        var organizationResult = await OrganizationLookup.LoadOrganizationForTenantAsync(
            _repository, request.OrganizationId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (organizationResult.IsFailure)
        {
            return Result.Failure<Guid>(organizationResult.Error);
        }

        var result = organizationResult.Value.AddDepartment(
            new DivisionId(request.DivisionId), request.Name, request.Code, _timeProvider.GetUtcNow());
        return result.IsFailure ? Result.Failure<Guid>(result.Error) : Result.Success(result.Value.Value);
    }
}

public sealed record RenameDepartmentCommand(
    Guid OrganizationId, Guid TenantId, Guid DepartmentId, string? NewName) : ICommand<Result>;

internal sealed class RenameDepartmentCommandHandler : IRequestHandler<RenameDepartmentCommand, Result>
{
    private readonly IOrganizationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RenameDepartmentCommandHandler(IOrganizationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RenameDepartmentCommand request, CancellationToken cancellationToken)
    {
        var organizationResult = await OrganizationLookup.LoadOrganizationForTenantAsync(
            _repository, request.OrganizationId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return organizationResult.IsFailure
            ? Result.Failure(organizationResult.Error)
            : organizationResult.Value.RenameDepartment(
                new DepartmentId(request.DepartmentId), request.NewName, _timeProvider.GetUtcNow());
    }
}

public sealed record MoveDepartmentCommand(
    Guid OrganizationId, Guid TenantId, Guid DepartmentId, Guid NewDivisionId) : ICommand<Result>;

internal sealed class MoveDepartmentCommandHandler : IRequestHandler<MoveDepartmentCommand, Result>
{
    private readonly IOrganizationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public MoveDepartmentCommandHandler(IOrganizationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(MoveDepartmentCommand request, CancellationToken cancellationToken)
    {
        var organizationResult = await OrganizationLookup.LoadOrganizationForTenantAsync(
            _repository, request.OrganizationId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return organizationResult.IsFailure
            ? Result.Failure(organizationResult.Error)
            : organizationResult.Value.MoveDepartment(
                new DepartmentId(request.DepartmentId), new DivisionId(request.NewDivisionId), _timeProvider.GetUtcNow());
    }
}

public sealed record MergeDepartmentsCommand(
    Guid OrganizationId, Guid TenantId, IReadOnlyList<Guid> SourceDepartmentIds, string? SurvivingName, string? SurvivingCode)
    : ICommand<Result<Guid>>;

internal sealed class MergeDepartmentsCommandHandler : IRequestHandler<MergeDepartmentsCommand, Result<Guid>>
{
    private readonly IOrganizationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public MergeDepartmentsCommandHandler(IOrganizationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(MergeDepartmentsCommand request, CancellationToken cancellationToken)
    {
        if (request.SurvivingCode is not null
            && await _repository.ExistsDepartmentWithCodeAsync(request.TenantId, request.SurvivingCode, null, cancellationToken)
                .ConfigureAwait(false))
        {
            return Result.Failure<Guid>(OrganizationErrors.DuplicateDepartmentCode);
        }

        var organizationResult = await OrganizationLookup.LoadOrganizationForTenantAsync(
            _repository, request.OrganizationId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (organizationResult.IsFailure)
        {
            return Result.Failure<Guid>(organizationResult.Error);
        }

        var sourceIds = request.SourceDepartmentIds.Select(id => new DepartmentId(id)).ToList();
        var result = organizationResult.Value.MergeDepartments(
            sourceIds, request.SurvivingName, request.SurvivingCode, _timeProvider.GetUtcNow());
        return result.IsFailure ? Result.Failure<Guid>(result.Error) : Result.Success(result.Value.Value);
    }
}

public sealed record NewDepartmentSpec(string? Name, string? Code);

public sealed record SplitDepartmentCommand(
    Guid OrganizationId, Guid TenantId, Guid SourceDepartmentId, IReadOnlyList<NewDepartmentSpec> NewDepartments)
    : ICommand<Result<IReadOnlyList<Guid>>>;

internal sealed class SplitDepartmentCommandHandler : IRequestHandler<SplitDepartmentCommand, Result<IReadOnlyList<Guid>>>
{
    private readonly IOrganizationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public SplitDepartmentCommandHandler(IOrganizationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<IReadOnlyList<Guid>>> Handle(SplitDepartmentCommand request, CancellationToken cancellationToken)
    {
        foreach (var spec in request.NewDepartments)
        {
            if (spec.Code is not null
                && await _repository.ExistsDepartmentWithCodeAsync(request.TenantId, spec.Code, null, cancellationToken)
                    .ConfigureAwait(false))
            {
                return Result.Failure<IReadOnlyList<Guid>>(OrganizationErrors.DuplicateDepartmentCode);
            }
        }

        var organizationResult = await OrganizationLookup.LoadOrganizationForTenantAsync(
            _repository, request.OrganizationId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (organizationResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<Guid>>(organizationResult.Error);
        }

        var newDepartments = request.NewDepartments.Select(s => (s.Name, s.Code)).ToList();
        var result = organizationResult.Value.SplitDepartment(
            new DepartmentId(request.SourceDepartmentId), newDepartments, _timeProvider.GetUtcNow());
        return result.IsFailure
            ? Result.Failure<IReadOnlyList<Guid>>(result.Error)
            : Result.Success<IReadOnlyList<Guid>>(result.Value.Select(id => id.Value).ToList());
    }
}

public sealed record ArchiveDepartmentCommand(Guid OrganizationId, Guid TenantId, Guid DepartmentId) : ICommand<Result>;

internal sealed class ArchiveDepartmentCommandHandler : IRequestHandler<ArchiveDepartmentCommand, Result>
{
    private readonly IOrganizationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ArchiveDepartmentCommandHandler(IOrganizationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ArchiveDepartmentCommand request, CancellationToken cancellationToken)
    {
        var organizationResult = await OrganizationLookup.LoadOrganizationForTenantAsync(
            _repository, request.OrganizationId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return organizationResult.IsFailure
            ? Result.Failure(organizationResult.Error)
            : organizationResult.Value.ArchiveDepartment(new DepartmentId(request.DepartmentId), _timeProvider.GetUtcNow());
    }
}

public sealed record RestoreDepartmentCommand(Guid OrganizationId, Guid TenantId, Guid DepartmentId) : ICommand<Result>;

internal sealed class RestoreDepartmentCommandHandler : IRequestHandler<RestoreDepartmentCommand, Result>
{
    private readonly IOrganizationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RestoreDepartmentCommandHandler(IOrganizationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RestoreDepartmentCommand request, CancellationToken cancellationToken)
    {
        var organizationResult = await OrganizationLookup.LoadOrganizationForTenantAsync(
            _repository, request.OrganizationId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return organizationResult.IsFailure
            ? Result.Failure(organizationResult.Error)
            : organizationResult.Value.RestoreDepartment(new DepartmentId(request.DepartmentId), _timeProvider.GetUtcNow());
    }
}
