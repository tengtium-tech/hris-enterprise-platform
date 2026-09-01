using Hris.Application.Abstractions;
using Hris.Foundation.Audit.Application.Dtos;
using Hris.Foundation.Audit.Application.Mapping;
using Hris.Foundation.Audit.Domain;
using Hris.Foundation.Authorization.Application.Queries;
using Hris.Foundation.Authorization.Domain;
using Hris.Foundation.Configuration.Application.Queries;
using Hris.Foundation.Configuration.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Audit.Application.Queries;

/// <summary>
/// Audit Search, per audit-framework.md's own section of the same name -- a thin
/// wrapper over <see cref="IAuditSearchService.SearchAsync"/>, gated by an explicit
/// authorization check: Security Considerations states "Only authorized users should
/// access audit information," and Authorization Framework's own Application layer now
/// exists to enforce that for real rather than deferring it (unlike, for example,
/// Logging Framework's own Identity/Authorization integration, deferred at the time it
/// was built because neither upstream framework had an Infrastructure layer yet).
///
/// <see cref="RequestingPrincipalId"/>/<see cref="ScopeLevel"/>/<see cref="ScopeId"/>
/// are supplied by the caller rather than resolved from ambient context, the same
/// caller-supplied-context precedent Identity Framework's own self-service commands
/// already establish (no request-scoped "current user" accessor exists yet in this
/// Sprint).
/// </summary>
public sealed record SearchAuditRecordsQuery(
    Guid RequestingPrincipalId,
    OrganizationalScopeLevel ScopeLevel,
    Guid ScopeId,
    AuditSearchCriteria Criteria,
    int PageNumber,
    int PageSize) : IQuery<Result<IReadOnlyList<AuditRecordDto>>>;

internal sealed class SearchAuditRecordsQueryHandler : IRequestHandler<SearchAuditRecordsQuery, Result<IReadOnlyList<AuditRecordDto>>>
{
    /// <summary>
    /// Resolved from Configuration Framework at Global scope, the same pattern
    /// AuthenticateCommandHandler and OutboxDispatcherBackgroundService already
    /// establish -- audit-framework.md's own Performance NFR ("minimal impact on
    /// business transactions") and Scalability NFR ("billions of audit records")
    /// together mean an unbounded search page size is exactly the kind of request a
    /// single caller could issue that degrades the platform for everyone else; capping
    /// it is not a later optimization pass.
    /// </summary>
    internal const string MaxPageSizeConfigurationKey = "Audit.MaxSearchPageSize";

    private const int _defaultMaxPageSize = 200;

    private readonly IAuditSearchService _searchService;
    private readonly ISender _sender;
    private readonly TimeProvider _timeProvider;

    public SearchAuditRecordsQueryHandler(IAuditSearchService searchService, ISender sender, TimeProvider timeProvider)
    {
        _searchService = Guard.AgainstNull(searchService, nameof(searchService));
        _sender = Guard.AgainstNull(sender, nameof(sender));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<IReadOnlyList<AuditRecordDto>>> Handle(
        SearchAuditRecordsQuery request, CancellationToken cancellationToken)
    {
        var authorizationResult = await _sender.Send(
            new CheckAuthorizationQuery(
                request.RequestingPrincipalId, "AuditRecord", PermissionAction.Read, request.ScopeLevel, request.ScopeId),
            cancellationToken).ConfigureAwait(false);

        if (authorizationResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<AuditRecordDto>>(authorizationResult.Error);
        }

        if (!authorizationResult.Value.IsAllowed)
        {
            return Result.Failure<IReadOnlyList<AuditRecordDto>>(AuditErrors.NotAuthorizedToAccessAuditRecords);
        }

        var maxPageSize = await ResolveMaxPageSizeAsync(cancellationToken).ConfigureAwait(false);
        var pageSize = Math.Clamp(request.PageSize, 1, maxPageSize);

        var records = await _searchService
            .SearchAsync(request.Criteria, request.PageNumber, pageSize, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<AuditRecordDto> dtos = records.Select(record => record.ToDto()).ToList();

        return Result.Success(dtos);
    }

    private async Task<int> ResolveMaxPageSizeAsync(CancellationToken cancellationToken)
    {
        var query = new ResolveConfigurationValueQuery(
            MaxPageSizeConfigurationKey, [ConfigurationScope.Global()], DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime));

        var result = await _sender.Send(query, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess && int.TryParse(result.Value, out var parsed) && parsed > 0
            ? parsed
            : _defaultMaxPageSize;
    }
}
