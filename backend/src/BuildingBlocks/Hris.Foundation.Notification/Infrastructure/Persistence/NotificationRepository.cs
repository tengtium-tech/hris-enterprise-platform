using Hris.Foundation.Notification.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.Notification.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="INotificationRepository"/>, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split.
/// </summary>
internal sealed class NotificationRepository : INotificationRepository
{
    private readonly HrisDbContext _dbContext;

    public NotificationRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<Domain.Notification?> GetByIdAsync(NotificationId id, CancellationToken cancellationToken) =>
        _dbContext.Set<Domain.Notification>().FirstOrDefaultAsync(notification => notification.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Domain.Notification> Items, int TotalCount, int UnreadCount)> ListInAppForRecipientAsync(
        Guid recipientUserId, Guid tenantId, bool? isRead, int skip, int take, CancellationToken cancellationToken)
    {
        var baseQuery = _dbContext.Set<Domain.Notification>().Where(notification =>
            notification.RecipientUserId == recipientUserId
            && notification.TenantId == tenantId
            && notification.Channel == NotificationChannel.InApp);

        var unreadCount = await baseQuery.CountAsync(
            notification => notification.Status != NotificationStatus.Read, cancellationToken).ConfigureAwait(false);

        var filteredQuery = isRead is null
            ? baseQuery
            : isRead.Value
                ? baseQuery.Where(notification => notification.Status == NotificationStatus.Read)
                : baseQuery.Where(notification => notification.Status != NotificationStatus.Read);

        var totalCount = await filteredQuery.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await filteredQuery
            .OrderByDescending(notification => notification.RequestedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items, totalCount, unreadCount);
    }

    public async Task AddAsync(Domain.Notification notification, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(notification, nameof(notification));
        await _dbContext.Set<Domain.Notification>().AddAsync(notification, cancellationToken).ConfigureAwait(false);
    }
}
