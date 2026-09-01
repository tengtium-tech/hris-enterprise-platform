using Hris.Foundation.Audit.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.Audit.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IAuditRecordRepository"/>. Deliberately
/// exposes only <see cref="GetByIdAsync"/> and <see cref="AddAsync"/>, matching that
/// interface's own contract exactly -- there is no <c>Update</c>/<c>Remove</c> method
/// to implement, which is the second, structural half of `CTR-AUD-001` that
/// interface's own remarks describe: even this Infrastructure class, with direct
/// <c>HrisDbContext</c> access, has no method here through which it could modify or
/// delete a persisted <see cref="AuditRecord"/>.
/// </summary>
internal sealed class AuditRecordRepository : IAuditRecordRepository
{
    private readonly HrisDbContext _dbContext;

    public AuditRecordRepository(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public Task<AuditRecord?> GetByIdAsync(AuditRecordId id, CancellationToken cancellationToken) =>
        _dbContext.Set<AuditRecord>()
            .FirstOrDefaultAsync(record => record.Id == id, cancellationToken);

    public async Task AddAsync(AuditRecord record, CancellationToken cancellationToken) =>
        await _dbContext.Set<AuditRecord>()
            .AddAsync(record, cancellationToken)
            .ConfigureAwait(false);
}
