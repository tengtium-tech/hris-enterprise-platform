using Hris.Foundation.Notification.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.Notification.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="INotificationPreferenceRepository"/>, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split.
/// </summary>
internal sealed class NotificationPreferenceRepository : INotificationPreferenceRepository
{
    private readonly HrisDbContext _dbContext;

    public NotificationPreferenceRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<NotificationPreference?> GetByIdAsync(NotificationPreferenceId id, CancellationToken cancellationToken) =>
        _dbContext.Set<NotificationPreference>().FirstOrDefaultAsync(preference => preference.Id == id, cancellationToken);

    public Task<NotificationPreference?> GetByUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken) =>
        _dbContext.Set<NotificationPreference>()
            .FirstOrDefaultAsync(preference => preference.TenantId == tenantId && preference.UserId == userId, cancellationToken);

    public Task<bool> ExistsByUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken) =>
        _dbContext.Set<NotificationPreference>()
            .AnyAsync(preference => preference.TenantId == tenantId && preference.UserId == userId, cancellationToken);

    public async Task AddAsync(NotificationPreference preference, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(preference, nameof(preference));
        await _dbContext.Set<NotificationPreference>().AddAsync(preference, cancellationToken).ConfigureAwait(false);
    }
}
