using Hris.Application.Abstractions;
using Hris.Foundation.Notification.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Notification.Application.Commands;

/// <summary>
/// notification-framework.md's own Client-Facing Commands and Queries section: "Notification,
/// actor (the recipient)." The one client-facing write this framework exposes -- <see cref="ActorUserId"/>
/// must be the notification's own recipient, enforced structurally by
/// <see cref="Domain.Notification.MarkRead"/> itself, not merely by this handler, per
/// that method's own remarks. Idempotent: marking an already-read notification succeeds
/// without change.
/// </summary>
public sealed record MarkNotificationReadCommand(Guid NotificationId, Guid ActorUserId) : ICommand<Result>;

internal sealed class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, Result>
{
    private readonly INotificationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public MarkNotificationReadCommandHandler(INotificationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await _repository.GetByIdAsync(
            new NotificationId(request.NotificationId), cancellationToken).ConfigureAwait(false);

        return notification is null
            ? Result.Failure(NotificationErrors.NotificationNotFound)
            : notification.MarkRead(request.ActorUserId, _timeProvider.GetUtcNow());
    }
}
