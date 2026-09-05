namespace Hris.Foundation.Notification.Domain;

/// <summary>
/// Repository interface in the Domain layer, implementation in Infrastructure, per
/// repositories.md's own split.
/// </summary>
public interface INotificationPreferenceRepository
{
    Task<NotificationPreference?> GetByIdAsync(NotificationPreferenceId id, CancellationToken cancellationToken);

    Task<NotificationPreference?> GetByUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken);

    Task<bool> ExistsByUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken);

    Task AddAsync(NotificationPreference preference, CancellationToken cancellationToken);
}
