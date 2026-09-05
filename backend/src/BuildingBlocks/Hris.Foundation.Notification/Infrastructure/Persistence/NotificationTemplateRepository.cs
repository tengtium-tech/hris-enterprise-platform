using Hris.Foundation.Notification.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.Notification.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="INotificationTemplateRepository"/>, per
/// repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split.
/// </summary>
internal sealed class NotificationTemplateRepository : INotificationTemplateRepository
{
    private readonly HrisDbContext _dbContext;

    public NotificationTemplateRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<NotificationTemplate?> GetByIdAsync(NotificationTemplateId id, CancellationToken cancellationToken) =>
        _dbContext.Set<NotificationTemplate>().FirstOrDefaultAsync(template => template.Id == id, cancellationToken);

    public Task<NotificationTemplate?> GetByTemplateKeyAsync(Guid tenantId, string templateKey, CancellationToken cancellationToken) =>
        _dbContext.Set<NotificationTemplate>()
            .FirstOrDefaultAsync(template => template.TenantId == tenantId && template.TemplateKey == templateKey, cancellationToken);

    public Task<bool> ExistsByTemplateKeyAsync(Guid tenantId, string templateKey, CancellationToken cancellationToken) =>
        _dbContext.Set<NotificationTemplate>()
            .AnyAsync(template => template.TenantId == tenantId && template.TemplateKey == templateKey, cancellationToken);

    public async Task<IReadOnlyList<NotificationTemplate>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await _dbContext.Set<NotificationTemplate>()
            .Where(template => template.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(NotificationTemplate notificationTemplate, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(notificationTemplate, nameof(notificationTemplate));
        await _dbContext.Set<NotificationTemplate>().AddAsync(notificationTemplate, cancellationToken).ConfigureAwait(false);
    }
}
