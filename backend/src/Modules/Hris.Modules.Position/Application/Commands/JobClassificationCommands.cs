using Hris.Application.Abstractions;
using Hris.Modules.Position.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Position.Application.Commands;

public sealed record CreateJobClassificationCommand(
    Guid TenantId, string? Code, string? Name, string? Description) : ICommand<Result<Guid>>;

internal sealed class CreateJobClassificationCommandHandler : IRequestHandler<CreateJobClassificationCommand, Result<Guid>>
{
    private readonly IJobClassificationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CreateJobClassificationCommandHandler(IJobClassificationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(CreateJobClassificationCommand request, CancellationToken cancellationToken)
    {
        if (request.Code is not null
            && await _repository.ExistsWithCodeAsync(request.TenantId, request.Code, null, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure<Guid>(PositionErrors.DuplicateJobClassificationCode);
        }

        if (request.Name is not null
            && await _repository.ExistsWithNameAsync(request.TenantId, request.Name, null, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure<Guid>(PositionErrors.DuplicateJobClassificationName);
        }

        var jobClassificationResult = JobClassification.Create(
            new JobClassificationId(Guid.NewGuid()), request.TenantId, request.Code, request.Name, request.Description,
            _timeProvider.GetUtcNow());
        if (jobClassificationResult.IsFailure)
        {
            return Result.Failure<Guid>(jobClassificationResult.Error);
        }

        await _repository.AddAsync(jobClassificationResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(jobClassificationResult.Value.Id.Value);
    }
}

public sealed record UpdateJobClassificationCommand(
    Guid JobClassificationId, Guid TenantId, string? Name, string? Description) : ICommand<Result>;

internal sealed class UpdateJobClassificationCommandHandler : IRequestHandler<UpdateJobClassificationCommand, Result>
{
    private readonly IJobClassificationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public UpdateJobClassificationCommandHandler(IJobClassificationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(UpdateJobClassificationCommand request, CancellationToken cancellationToken)
    {
        if (request.Name is not null
            && await _repository.ExistsWithNameAsync(
                request.TenantId, request.Name, new JobClassificationId(request.JobClassificationId), cancellationToken)
                .ConfigureAwait(false))
        {
            return Result.Failure(PositionErrors.DuplicateJobClassificationName);
        }

        var jobClassificationResult = await PositionLookup.LoadJobClassificationForTenantAsync(
            _repository, request.JobClassificationId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return jobClassificationResult.IsFailure
            ? Result.Failure(jobClassificationResult.Error)
            : jobClassificationResult.Value.Update(request.Name, request.Description, _timeProvider.GetUtcNow());
    }
}

public sealed record ActivateJobClassificationCommand(Guid JobClassificationId, Guid TenantId) : ICommand<Result>;

internal sealed class ActivateJobClassificationCommandHandler : IRequestHandler<ActivateJobClassificationCommand, Result>
{
    private readonly IJobClassificationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ActivateJobClassificationCommandHandler(IJobClassificationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ActivateJobClassificationCommand request, CancellationToken cancellationToken)
    {
        var jobClassificationResult = await PositionLookup.LoadJobClassificationForTenantAsync(
            _repository, request.JobClassificationId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return jobClassificationResult.IsFailure
            ? Result.Failure(jobClassificationResult.Error)
            : jobClassificationResult.Value.Activate(_timeProvider.GetUtcNow());
    }
}

public sealed record DeactivateJobClassificationCommand(Guid JobClassificationId, Guid TenantId) : ICommand<Result>;

internal sealed class DeactivateJobClassificationCommandHandler : IRequestHandler<DeactivateJobClassificationCommand, Result>
{
    private readonly IJobClassificationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public DeactivateJobClassificationCommandHandler(IJobClassificationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(DeactivateJobClassificationCommand request, CancellationToken cancellationToken)
    {
        var jobClassificationResult = await PositionLookup.LoadJobClassificationForTenantAsync(
            _repository, request.JobClassificationId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return jobClassificationResult.IsFailure
            ? Result.Failure(jobClassificationResult.Error)
            : jobClassificationResult.Value.Deactivate(_timeProvider.GetUtcNow());
    }
}

public sealed record ArchiveJobClassificationCommand(Guid JobClassificationId, Guid TenantId) : ICommand<Result>;

internal sealed class ArchiveJobClassificationCommandHandler : IRequestHandler<ArchiveJobClassificationCommand, Result>
{
    private readonly IJobClassificationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ArchiveJobClassificationCommandHandler(IJobClassificationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ArchiveJobClassificationCommand request, CancellationToken cancellationToken)
    {
        var jobClassificationResult = await PositionLookup.LoadJobClassificationForTenantAsync(
            _repository, request.JobClassificationId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return jobClassificationResult.IsFailure
            ? Result.Failure(jobClassificationResult.Error)
            : jobClassificationResult.Value.Archive(_timeProvider.GetUtcNow());
    }
}
