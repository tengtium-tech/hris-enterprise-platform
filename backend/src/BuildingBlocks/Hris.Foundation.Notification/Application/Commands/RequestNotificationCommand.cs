using Hris.Application.Abstractions;
using Hris.Foundation.Notification.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Notification.Application.Commands;

/// <summary>
/// The path a business module actually uses to originate a notification --
/// notification-framework.md's own Client-Facing Commands section states plainly
/// there is no client-facing <c>CreateNotificationCommand</c>; this command exists for
/// the same purpose that one would have, but is reached only through another module's
/// own MediatR request, never exposed as a public API endpoint (no Sprint 4/5 framework
/// in this codebase has built actual HTTP endpoints yet -- that is Sprint 7's own API
/// Platform concern).
/// </summary>
public sealed record RequestNotificationCommand(
    Guid TenantId,
    Guid RecipientUserId,
    NotificationType NotificationType,
    NotificationChannel Channel,
    string? TemplateKey,
    string? Subject,
    string Body) : ICommand<Result<Guid>>;

internal sealed class RequestNotificationCommandHandler : IRequestHandler<RequestNotificationCommand, Result<Guid>>
{
    private readonly INotificationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RequestNotificationCommandHandler(INotificationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(RequestNotificationCommand request, CancellationToken cancellationToken)
    {
        var createResult = Domain.Notification.Request(
            request.TenantId, request.RecipientUserId, request.NotificationType, request.Channel,
            request.TemplateKey, request.Subject, request.Body, _timeProvider.GetUtcNow());
        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Error);
        }

        await _repository.AddAsync(createResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(createResult.Value.Id.Value);
    }
}
