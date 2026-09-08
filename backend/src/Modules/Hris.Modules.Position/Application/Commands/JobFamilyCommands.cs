using Hris.Application.Abstractions;
using Hris.Modules.Position.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Position.Application.Commands;

public sealed record CreateJobFamilyCommand(Guid TenantId, string? Code, string? Name, string? Description) : ICommand<Result<Guid>>;

internal sealed class CreateJobFamilyCommandHandler : IRequestHandler<CreateJobFamilyCommand, Result<Guid>>
{
    private readonly IJobFamilyRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CreateJobFamilyCommandHandler(IJobFamilyRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(CreateJobFamilyCommand request, CancellationToken cancellationToken)
    {
        if (request.Code is not null
            && await _repository.ExistsWithCodeAsync(request.TenantId, request.Code, null, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure<Guid>(PositionErrors.DuplicateJobFamilyCode);
        }

        if (request.Name is not null
            && await _repository.ExistsWithNameAsync(request.TenantId, request.Name, null, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure<Guid>(PositionErrors.DuplicateJobFamilyName);
        }

        var jobFamilyResult = JobFamily.Create(
            new JobFamilyId(Guid.NewGuid()), request.TenantId, request.Code, request.Name, request.Description,
            _timeProvider.GetUtcNow());
        if (jobFamilyResult.IsFailure)
        {
            return Result.Failure<Guid>(jobFamilyResult.Error);
        }

        await _repository.AddAsync(jobFamilyResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(jobFamilyResult.Value.Id.Value);
    }
}

public sealed record UpdateJobFamilyCommand(Guid JobFamilyId, Guid TenantId, string? Name, string? Description) : ICommand<Result>;

internal sealed class UpdateJobFamilyCommandHandler : IRequestHandler<UpdateJobFamilyCommand, Result>
{
    private readonly IJobFamilyRepository _repository;
    private readonly TimeProvider _timeProvider;

    public UpdateJobFamilyCommandHandler(IJobFamilyRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(UpdateJobFamilyCommand request, CancellationToken cancellationToken)
    {
        if (request.Name is not null
            && await _repository.ExistsWithNameAsync(
                request.TenantId, request.Name, new JobFamilyId(request.JobFamilyId), cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure(PositionErrors.DuplicateJobFamilyName);
        }

        var jobFamilyResult = await PositionLookup.LoadJobFamilyForTenantAsync(
            _repository, request.JobFamilyId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return jobFamilyResult.IsFailure
            ? Result.Failure(jobFamilyResult.Error)
            : jobFamilyResult.Value.Update(request.Name, request.Description, _timeProvider.GetUtcNow());
    }
}

public sealed record ActivateJobFamilyCommand(Guid JobFamilyId, Guid TenantId) : ICommand<Result>;

internal sealed class ActivateJobFamilyCommandHandler : IRequestHandler<ActivateJobFamilyCommand, Result>
{
    private readonly IJobFamilyRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ActivateJobFamilyCommandHandler(IJobFamilyRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ActivateJobFamilyCommand request, CancellationToken cancellationToken)
    {
        var jobFamilyResult = await PositionLookup.LoadJobFamilyForTenantAsync(
            _repository, request.JobFamilyId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return jobFamilyResult.IsFailure
            ? Result.Failure(jobFamilyResult.Error)
            : jobFamilyResult.Value.Activate(_timeProvider.GetUtcNow());
    }
}

public sealed record DeactivateJobFamilyCommand(Guid JobFamilyId, Guid TenantId) : ICommand<Result>;

internal sealed class DeactivateJobFamilyCommandHandler : IRequestHandler<DeactivateJobFamilyCommand, Result>
{
    private readonly IJobFamilyRepository _repository;
    private readonly TimeProvider _timeProvider;

    public DeactivateJobFamilyCommandHandler(IJobFamilyRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(DeactivateJobFamilyCommand request, CancellationToken cancellationToken)
    {
        var jobFamilyResult = await PositionLookup.LoadJobFamilyForTenantAsync(
            _repository, request.JobFamilyId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return jobFamilyResult.IsFailure
            ? Result.Failure(jobFamilyResult.Error)
            : jobFamilyResult.Value.Deactivate(_timeProvider.GetUtcNow());
    }
}

public sealed record ArchiveJobFamilyCommand(Guid JobFamilyId, Guid TenantId) : ICommand<Result>;

internal sealed class ArchiveJobFamilyCommandHandler : IRequestHandler<ArchiveJobFamilyCommand, Result>
{
    private readonly IJobFamilyRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ArchiveJobFamilyCommandHandler(IJobFamilyRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ArchiveJobFamilyCommand request, CancellationToken cancellationToken)
    {
        var jobFamilyResult = await PositionLookup.LoadJobFamilyForTenantAsync(
            _repository, request.JobFamilyId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return jobFamilyResult.IsFailure
            ? Result.Failure(jobFamilyResult.Error)
            : jobFamilyResult.Value.Archive(_timeProvider.GetUtcNow());
    }
}
