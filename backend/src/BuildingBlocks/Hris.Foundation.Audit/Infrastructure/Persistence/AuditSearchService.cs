using Hris.Foundation.Audit.Domain;
using Hris.Foundation.Identity.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hris.Foundation.Audit.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IAuditSearchService"/>, reading from the same
/// store <see cref="AuditRecordRepository"/> writes to -- that interface's own remarks
/// explicitly leave this choice open; a dedicated read index is a future option, not
/// a requirement this Sprint has a reason to build yet.
/// </summary>
/// <remarks>
/// <see cref="AuditSearchCriteria.CompanyId"/>, <see cref="AuditSearchCriteria.DepartmentId"/>,
/// and <see cref="AuditSearchCriteria.Location"/> are accepted but not applied here --
/// <see cref="AuditRecord"/> itself carries no organizational-scope field of its own
/// to filter on (Organization does not exist until Phase 2, `CTR-ARC-002`, the same
/// reasoning that criteria type's own remarks already give for keeping those three
/// fields as raw, optional identifiers rather than strongly typed ones). Stated here
/// rather than silently ignored: a caller supplying one of these three filters gets
/// results that are not actually scoped by it, which is a real, load-bearing gap this
/// comment exists so the next implementer does not miss.
///
/// VERIFIED: the <c>record.CorrelationId == correlationId</c> predicate compares a
/// converted <see cref="CorrelationId"/> Value Object to a constant -- the identical
/// shape <c>ConfigurationSettingRepository</c>'s own remarks already confirmed
/// (HEP-38). Confirmed here too, against a real, disposable PostgreSQL 16 instance
/// via Testcontainers -- see
/// <c>Hris.Infrastructure.IntegrationTests.RepositoryQueryTranslationTests.AuditSearchService_SearchAsync_TranslatesCorrelationIdComparison</c>.
/// Passes: no fix needed.
/// </remarks>
internal sealed class AuditSearchService : IAuditSearchService
{
    private readonly HrisDbContext _dbContext;

    public AuditSearchService(HrisDbContext dbContext)
    {
        _dbContext = Guard.AgainstNull(dbContext, nameof(dbContext));
    }

    public async Task<IReadOnlyList<AuditRecord>> SearchAsync(
        AuditSearchCriteria criteria, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(criteria, nameof(criteria));

        var query = _dbContext.Set<AuditRecord>().AsQueryable();

        if (criteria.FromDate is not null)
        {
            var fromUtc = criteria.FromDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(record => record.TimestampUtc >= fromUtc);
        }

        if (criteria.ToDate is not null)
        {
            var toUtc = criteria.ToDate.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            query = query.Where(record => record.TimestampUtc <= toUtc);
        }

        if (criteria.ActorId is not null)
        {
            var actorId = criteria.ActorId.Value;
            query = query.Where(record => record.ActorId == actorId);
        }

        if (!string.IsNullOrWhiteSpace(criteria.BusinessEntity))
        {
            query = query.Where(record => record.BusinessEntity == criteria.BusinessEntity);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Action))
        {
            query = query.Where(record => record.Action == criteria.Action);
        }

        if (criteria.CorrelationId is not null)
        {
            var correlationIdResult = CorrelationId.Create(criteria.CorrelationId.Value);
            if (correlationIdResult.IsSuccess)
            {
                var correlationId = correlationIdResult.Value;
                query = query.Where(record => record.CorrelationId == correlationId);
            }
        }

        if (criteria.Outcome is not null)
        {
            query = query.Where(record => record.Outcome == criteria.Outcome.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.SourceSystem))
        {
            query = query.Where(record => record.SourceSystem == criteria.SourceSystem);
        }

        return await query
            .OrderByDescending(record => record.TimestampUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
