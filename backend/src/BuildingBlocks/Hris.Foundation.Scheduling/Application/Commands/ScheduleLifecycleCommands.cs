using Hris.Application.Abstractions;
using Hris.Foundation.Scheduling.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Scheduling.Application.Commands;

/// <summary>
/// The six remaining <see cref="Schedule"/>-level lifecycle transitions -- validate,
/// approve, activate, pause, resume, retire -- grouped into one file the same way every
/// other Sprint 3/4 framework's own bundled lifecycle commands are (see
/// <c>NumberSeriesLifecycleCommands</c>).
/// </summary>
public sealed record ValidateScheduleCommand(Guid ScheduleId) : ICommand<Result>;

internal sealed class ValidateScheduleCommandHandler : IRequestHandler<ValidateScheduleCommand, Result>
{
    private readonly IScheduleRepository _repository;

    public ValidateScheduleCommandHandler(IScheduleRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result> Handle(ValidateScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await _repository.GetByIdAsync(new ScheduleId(request.ScheduleId), cancellationToken).ConfigureAwait(false);
        return schedule is null ? Result.Failure(SchedulingErrors.ScheduleNotFound) : schedule.Validate();
    }
}

public sealed record ApproveScheduleCommand(Guid ScheduleId) : ICommand<Result>;

internal sealed class ApproveScheduleCommandHandler : IRequestHandler<ApproveScheduleCommand, Result>
{
    private readonly IScheduleRepository _repository;

    public ApproveScheduleCommandHandler(IScheduleRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result> Handle(ApproveScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await _repository.GetByIdAsync(new ScheduleId(request.ScheduleId), cancellationToken).ConfigureAwait(false);
        return schedule is null ? Result.Failure(SchedulingErrors.ScheduleNotFound) : schedule.Approve();
    }
}

public sealed record ActivateScheduleCommand(Guid ScheduleId) : ICommand<Result>;

internal sealed class ActivateScheduleCommandHandler : IRequestHandler<ActivateScheduleCommand, Result>
{
    private readonly IScheduleRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ActivateScheduleCommandHandler(IScheduleRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ActivateScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await _repository.GetByIdAsync(new ScheduleId(request.ScheduleId), cancellationToken).ConfigureAwait(false);
        return schedule is null ? Result.Failure(SchedulingErrors.ScheduleNotFound) : schedule.Activate(_timeProvider.GetUtcNow());
    }
}

public sealed record PauseScheduleCommand(Guid ScheduleId) : ICommand<Result>;

internal sealed class PauseScheduleCommandHandler : IRequestHandler<PauseScheduleCommand, Result>
{
    private readonly IScheduleRepository _repository;
    private readonly TimeProvider _timeProvider;

    public PauseScheduleCommandHandler(IScheduleRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(PauseScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await _repository.GetByIdAsync(new ScheduleId(request.ScheduleId), cancellationToken).ConfigureAwait(false);
        return schedule is null ? Result.Failure(SchedulingErrors.ScheduleNotFound) : schedule.Pause(_timeProvider.GetUtcNow());
    }
}

public sealed record ResumeScheduleCommand(Guid ScheduleId) : ICommand<Result>;

internal sealed class ResumeScheduleCommandHandler : IRequestHandler<ResumeScheduleCommand, Result>
{
    private readonly IScheduleRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ResumeScheduleCommandHandler(IScheduleRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ResumeScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await _repository.GetByIdAsync(new ScheduleId(request.ScheduleId), cancellationToken).ConfigureAwait(false);
        return schedule is null ? Result.Failure(SchedulingErrors.ScheduleNotFound) : schedule.Resume(_timeProvider.GetUtcNow());
    }
}

public sealed record RetireScheduleCommand(Guid ScheduleId) : ICommand<Result>;

internal sealed class RetireScheduleCommandHandler : IRequestHandler<RetireScheduleCommand, Result>
{
    private readonly IScheduleRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RetireScheduleCommandHandler(IScheduleRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RetireScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await _repository.GetByIdAsync(new ScheduleId(request.ScheduleId), cancellationToken).ConfigureAwait(false);
        return schedule is null ? Result.Failure(SchedulingErrors.ScheduleNotFound) : schedule.Retire(_timeProvider.GetUtcNow());
    }
}
