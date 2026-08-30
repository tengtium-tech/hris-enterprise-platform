namespace Hris.Foundation.Audit.Domain;

/// <summary>
/// The read side of audit-framework.md's Audit Search and Audit Reporting sections,
/// kept separate from <see cref="IAuditRecordRepository"/> per repositories.md's
/// Query Separation principle. An Infrastructure implementation is free to serve
/// this from a purpose-built read index rather than the same store
/// <see cref="IAuditRecordRepository"/> writes to -- this interface makes no
/// assumption either way.
/// </summary>
public interface IAuditSearchService
{
    Task<IReadOnlyList<AuditRecord>> SearchAsync(
        AuditSearchCriteria criteria,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);
}
