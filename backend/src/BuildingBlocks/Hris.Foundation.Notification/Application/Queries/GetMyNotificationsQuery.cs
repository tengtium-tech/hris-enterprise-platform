using Hris.Application.Abstractions;
using Hris.Foundation.Notification.Application.Dtos;
using Hris.Foundation.Notification.Application.Mapping;
using Hris.Foundation.Notification.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Notification.Application.Queries;

/// <summary>
/// notification-framework.md's own Client-Facing Commands and Queries section: "The
/// caller's own in-app notifications, filterable by Read/unread, paged; the unread
/// count... is derived from this same result, not a separate query." Always scoped to
/// <see cref="RecipientUserId"/> within <see cref="TenantId"/> -- there is deliberately
/// no equivalent taking a target user, per that section's own "no admin-override path"
/// requirement.
/// </summary>
public sealed record GetMyNotificationsQuery(
    Guid RecipientUserId,
    Guid TenantId,
    bool? IsRead,
    int Skip,
    int Take) : IQuery<Result<GetMyNotificationsResultDto>>;

internal sealed class GetMyNotificationsQueryHandler : IRequestHandler<GetMyNotificationsQuery, Result<GetMyNotificationsResultDto>>
{
    private readonly INotificationRepository _repository;

    public GetMyNotificationsQueryHandler(INotificationRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<GetMyNotificationsResultDto>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount, unreadCount) = await _repository.ListInAppForRecipientAsync(
            request.RecipientUserId, request.TenantId, request.IsRead, request.Skip, request.Take, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(new GetMyNotificationsResultDto(items.Select(NotificationMapper.ToDto).ToList(), totalCount, unreadCount));
    }
}
