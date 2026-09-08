using Hris.Application.Abstractions;
using Hris.Modules.Position.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Position.Application.Commands;

public sealed record CreateJobGradeCommand(
    Guid TenantId, string? Code, string? Name, string? Description, int? OrganizationalLevel) : ICommand<Result<Guid>>;

internal sealed class CreateJobGradeCommandHandler : IRequestHandler<CreateJobGradeCommand, Result<Guid>>
{
    private readonly IJobGradeRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CreateJobGradeCommandHandler(IJobGradeRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(CreateJobGradeCommand request, CancellationToken cancellationToken)
    {
        if (request.Code is not null
            && await _repository.ExistsWithCodeAsync(request.TenantId, request.Code, null, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure<Guid>(PositionErrors.DuplicateJobGradeCode);
        }

        if (request.Name is not null
            && await _repository.ExistsWithNameAsync(request.TenantId, request.Name, null, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure<Guid>(PositionErrors.DuplicateJobGradeName);
        }

        var jobGradeResult = JobGrade.Create(
            new JobGradeId(Guid.NewGuid()), request.TenantId, request.Code, request.Name, request.Description,
            request.OrganizationalLevel, _timeProvider.GetUtcNow());
        if (jobGradeResult.IsFailure)
        {
            return Result.Failure<Guid>(jobGradeResult.Error);
        }

        await _repository.AddAsync(jobGradeResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(jobGradeResult.Value.Id.Value);
    }
}

public sealed record UpdateJobGradeCommand(
    Guid JobGradeId, Guid TenantId, string? Name, string? Description, int? OrganizationalLevel) : ICommand<Result>;

internal sealed class UpdateJobGradeCommandHandler : IRequestHandler<UpdateJobGradeCommand, Result>
{
    private readonly IJobGradeRepository _repository;
    private readonly TimeProvider _timeProvider;

    public UpdateJobGradeCommandHandler(IJobGradeRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(UpdateJobGradeCommand request, CancellationToken cancellationToken)
    {
        if (request.Name is not null
            && await _repository.ExistsWithNameAsync(
                request.TenantId, request.Name, new JobGradeId(request.JobGradeId), cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure(PositionErrors.DuplicateJobGradeName);
        }

        var jobGradeResult = await PositionLookup.LoadJobGradeForTenantAsync(
            _repository, request.JobGradeId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return jobGradeResult.IsFailure
            ? Result.Failure(jobGradeResult.Error)
            : jobGradeResult.Value.Update(request.Name, request.Description, request.OrganizationalLevel, _timeProvider.GetUtcNow());
    }
}

public sealed record ActivateJobGradeCommand(Guid JobGradeId, Guid TenantId) : ICommand<Result>;

internal sealed class ActivateJobGradeCommandHandler : IRequestHandler<ActivateJobGradeCommand, Result>
{
    private readonly IJobGradeRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ActivateJobGradeCommandHandler(IJobGradeRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ActivateJobGradeCommand request, CancellationToken cancellationToken)
    {
        var jobGradeResult = await PositionLookup.LoadJobGradeForTenantAsync(
            _repository, request.JobGradeId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return jobGradeResult.IsFailure
            ? Result.Failure(jobGradeResult.Error)
            : jobGradeResult.Value.Activate(_timeProvider.GetUtcNow());
    }
}

public sealed record DeactivateJobGradeCommand(Guid JobGradeId, Guid TenantId) : ICommand<Result>;

internal sealed class DeactivateJobGradeCommandHandler : IRequestHandler<DeactivateJobGradeCommand, Result>
{
    private readonly IJobGradeRepository _repository;
    private readonly TimeProvider _timeProvider;

    public DeactivateJobGradeCommandHandler(IJobGradeRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(DeactivateJobGradeCommand request, CancellationToken cancellationToken)
    {
        var jobGradeResult = await PositionLookup.LoadJobGradeForTenantAsync(
            _repository, request.JobGradeId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return jobGradeResult.IsFailure
            ? Result.Failure(jobGradeResult.Error)
            : jobGradeResult.Value.Deactivate(_timeProvider.GetUtcNow());
    }
}

public sealed record ArchiveJobGradeCommand(Guid JobGradeId, Guid TenantId) : ICommand<Result>;

internal sealed class ArchiveJobGradeCommandHandler : IRequestHandler<ArchiveJobGradeCommand, Result>
{
    private readonly IJobGradeRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ArchiveJobGradeCommandHandler(IJobGradeRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ArchiveJobGradeCommand request, CancellationToken cancellationToken)
    {
        var jobGradeResult = await PositionLookup.LoadJobGradeForTenantAsync(
            _repository, request.JobGradeId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return jobGradeResult.IsFailure
            ? Result.Failure(jobGradeResult.Error)
            : jobGradeResult.Value.Archive(_timeProvider.GetUtcNow());
    }
}
