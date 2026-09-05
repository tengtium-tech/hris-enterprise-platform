namespace Hris.Foundation.Notification.Domain;

/// <summary>
/// Repository interface in the Domain layer, implementation in Infrastructure, per
/// repositories.md's own split.
/// </summary>
public interface INotificationTemplateRepository
{
    Task<NotificationTemplate?> GetByIdAsync(NotificationTemplateId id, CancellationToken cancellationToken);

    Task<NotificationTemplate?> GetByTemplateKeyAsync(Guid tenantId, string templateKey, CancellationToken cancellationToken);

    Task<bool> ExistsByTemplateKeyAsync(Guid tenantId, string templateKey, CancellationToken cancellationToken);

    Task<IReadOnlyList<NotificationTemplate>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task AddAsync(NotificationTemplate notificationTemplate, CancellationToken cancellationToken);
}
