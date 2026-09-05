using Hris.Application.Abstractions;
using Hris.Foundation.Notification.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Notification.Application.Commands;

/// <summary>
/// Every <see cref="Domain.Notification"/> lifecycle transition other than
/// <see cref="RequestNotificationCommand"/> and <c>MarkNotificationReadCommand</c> (its
/// own client-facing file, per notification-framework.md's own remarks) bundled into
/// one file -- the identical shape <c>WorkflowInstanceLifecycleCommands.cs</c> already
/// establishes for its own sibling aggregate.
/// </summary>
public sealed record QueueNotificationCommand(Guid NotificationId) : ICommand<Result>;

public sealed record ScheduleNotificationCommand(Guid NotificationId, DateTimeOffset ScheduledForUtc) : ICommand<Result>;

public sealed record StartProcessingNotificationCommand(Guid NotificationId) : ICommand<Result>;

public sealed record MarkNotificationSentCommand(Guid NotificationId) : ICommand<Result>;

public sealed record MarkNotificationDeliveredCommand(Guid NotificationId) : ICommand<Result>;

public sealed record AcknowledgeNotificationCommand(Guid NotificationId) : ICommand<Result>;

public sealed record FailNotificationCommand(Guid NotificationId, string Reason) : ICommand<Result>;

public sealed record RetryNotificationCommand(Guid NotificationId) : ICommand<Result>;

public sealed record MoveNotificationToDeadLetterCommand(Guid NotificationId) : ICommand<Result>;

public sealed record ExpireNotificationCommand(Guid NotificationId) : ICommand<Result>;

public sealed record SuppressNotificationCommand(Guid NotificationId) : ICommand<Result>;

public sealed record CancelNotificationCommand(Guid NotificationId, string Reason) : ICommand<Result>;

internal sealed class QueueNotificationCommandHandler : IRequestHandler<QueueNotificationCommand, Result>
{
    private readonly INotificationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public QueueNotificationCommandHandler(INotificationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(QueueNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = await _repository.GetByIdAsync(new NotificationId(request.NotificationId), cancellationToken).ConfigureAwait(false);
        return notification is null
            ? Result.Failure(NotificationErrors.NotificationNotFound)
            : notification.Queue(_timeProvider.GetUtcNow());
    }
}

internal sealed class ScheduleNotificationCommandHandler : IRequestHandler<ScheduleNotificationCommand, Result>
{
    private readonly INotificationRepository _repository;

    public ScheduleNotificationCommandHandler(INotificationRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result> Handle(ScheduleNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = await _repository.GetByIdAsync(new NotificationId(request.NotificationId), cancellationToken).ConfigureAwait(false);
        return notification is null
            ? Result.Failure(NotificationErrors.NotificationNotFound)
            : notification.Schedule(request.ScheduledForUtc);
    }
}

internal sealed class StartProcessingNotificationCommandHandler : IRequestHandler<StartProcessingNotificationCommand, Result>
{
    private readonly INotificationRepository _repository;

    public StartProcessingNotificationCommandHandler(INotificationRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result> Handle(StartProcessingNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = await _repository.GetByIdAsync(new NotificationId(request.NotificationId), cancellationToken).ConfigureAwait(false);
        return notification is null ? Result.Failure(NotificationErrors.NotificationNotFound) : notification.StartProcessing();
    }
}

internal sealed class MarkNotificationSentCommandHandler : IRequestHandler<MarkNotificationSentCommand, Result>
{
    private readonly INotificationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public MarkNotificationSentCommandHandler(INotificationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(MarkNotificationSentCommand request, CancellationToken cancellationToken)
    {
        var notification = await _repository.GetByIdAsync(new NotificationId(request.NotificationId), cancellationToken).ConfigureAwait(false);
        return notification is null
            ? Result.Failure(NotificationErrors.NotificationNotFound)
            : notification.MarkSent(_timeProvider.GetUtcNow());
    }
}

internal sealed class MarkNotificationDeliveredCommandHandler : IRequestHandler<MarkNotificationDeliveredCommand, Result>
{
    private readonly INotificationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public MarkNotificationDeliveredCommandHandler(INotificationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(MarkNotificationDeliveredCommand request, CancellationToken cancellationToken)
    {
        var notification = await _repository.GetByIdAsync(new NotificationId(request.NotificationId), cancellationToken).ConfigureAwait(false);
        return notification is null
            ? Result.Failure(NotificationErrors.NotificationNotFound)
            : notification.MarkDelivered(_timeProvider.GetUtcNow());
    }
}

internal sealed class AcknowledgeNotificationCommandHandler : IRequestHandler<AcknowledgeNotificationCommand, Result>
{
    private readonly INotificationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public AcknowledgeNotificationCommandHandler(INotificationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(AcknowledgeNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = await _repository.GetByIdAsync(new NotificationId(request.NotificationId), cancellationToken).ConfigureAwait(false);
        return notification is null
            ? Result.Failure(NotificationErrors.NotificationNotFound)
            : notification.Acknowledge(_timeProvider.GetUtcNow());
    }
}

internal sealed class FailNotificationCommandHandler : IRequestHandler<FailNotificationCommand, Result>
{
    private readonly INotificationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public FailNotificationCommandHandler(INotificationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(FailNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = await _repository.GetByIdAsync(new NotificationId(request.NotificationId), cancellationToken).ConfigureAwait(false);
        return notification is null
            ? Result.Failure(NotificationErrors.NotificationNotFound)
            : notification.Fail(request.Reason, _timeProvider.GetUtcNow());
    }
}

internal sealed class RetryNotificationCommandHandler : IRequestHandler<RetryNotificationCommand, Result>
{
    private readonly INotificationRepository _repository;

    public RetryNotificationCommandHandler(INotificationRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result> Handle(RetryNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = await _repository.GetByIdAsync(new NotificationId(request.NotificationId), cancellationToken).ConfigureAwait(false);
        return notification is null ? Result.Failure(NotificationErrors.NotificationNotFound) : notification.RetryAfterFailure();
    }
}

internal sealed class MoveNotificationToDeadLetterCommandHandler : IRequestHandler<MoveNotificationToDeadLetterCommand, Result>
{
    private readonly INotificationRepository _repository;

    public MoveNotificationToDeadLetterCommandHandler(INotificationRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result> Handle(MoveNotificationToDeadLetterCommand request, CancellationToken cancellationToken)
    {
        var notification = await _repository.GetByIdAsync(new NotificationId(request.NotificationId), cancellationToken).ConfigureAwait(false);
        return notification is null ? Result.Failure(NotificationErrors.NotificationNotFound) : notification.MoveToDeadLetter();
    }
}

internal sealed class ExpireNotificationCommandHandler : IRequestHandler<ExpireNotificationCommand, Result>
{
    private readonly INotificationRepository _repository;

    public ExpireNotificationCommandHandler(INotificationRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result> Handle(ExpireNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = await _repository.GetByIdAsync(new NotificationId(request.NotificationId), cancellationToken).ConfigureAwait(false);
        return notification is null ? Result.Failure(NotificationErrors.NotificationNotFound) : notification.Expire();
    }
}

internal sealed class SuppressNotificationCommandHandler : IRequestHandler<SuppressNotificationCommand, Result>
{
    private readonly INotificationRepository _repository;

    public SuppressNotificationCommandHandler(INotificationRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result> Handle(SuppressNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = await _repository.GetByIdAsync(new NotificationId(request.NotificationId), cancellationToken).ConfigureAwait(false);
        return notification is null ? Result.Failure(NotificationErrors.NotificationNotFound) : notification.Suppress();
    }
}

internal sealed class CancelNotificationCommandHandler : IRequestHandler<CancelNotificationCommand, Result>
{
    private readonly INotificationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CancelNotificationCommandHandler(INotificationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(CancelNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = await _repository.GetByIdAsync(new NotificationId(request.NotificationId), cancellationToken).ConfigureAwait(false);
        return notification is null
            ? Result.Failure(NotificationErrors.NotificationNotFound)
            : notification.Cancel(request.Reason, _timeProvider.GetUtcNow());
    }
}
