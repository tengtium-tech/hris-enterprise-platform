using Hris.SharedKernel;

namespace Hris.Foundation.Search.Domain;

/// <summary>
/// Aggregate Root for one search request/response cycle -- see
/// <see cref="SearchExecutionId"/>'s own remarks for why this exists as a real
/// aggregate rather than a log line. Population-scale in the same sense
/// <see cref="IndexedDocument"/> is: search-framework.md's own Non-Functional
/// Requirements state "high query volumes across multiple tenants."
/// </summary>
public sealed class SearchExecution : AggregateRoot<SearchExecutionId>
{
    public Guid TenantId { get; }

    public Guid RequestedByUserId { get; }

    public string QueryText { get; }

    public string? DomainFilter { get; }

    public SearchExecutionStatus Status { get; private set; }

    public int? ResultCount { get; private set; }

    public long? LatencyMs { get; private set; }

    public string? FailureReason { get; private set; }

    public DateTimeOffset RequestedAtUtc { get; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    private SearchExecution(
        SearchExecutionId id, Guid tenantId, Guid requestedByUserId, string queryText, string? domainFilter, DateTimeOffset nowUtc)
        : base(id)
    {
        TenantId = tenantId;
        RequestedByUserId = requestedByUserId;
        QueryText = queryText;
        DomainFilter = domainFilter;
        Status = SearchExecutionStatus.Requested;
        RequestedAtUtc = nowUtc;
    }

    /// <summary>
    /// EF Core materialization only -- never called by application code, which always
    /// goes through the constructor above via <see cref="Request"/>. The constructor
    /// above takes <c>nowUtc</c>, which does not share a name with the property it
    /// sets (<see cref="RequestedAtUtc"/>), so EF Core's own constructor-binding
    /// convention cannot bind it -- the identical failure shape <see cref="IndexedDocument"/>'s
    /// own second constructor works around, found the same way (a real model build
    /// failing with "No suitable constructor was found").
    /// </summary>
    private SearchExecution(
        SearchExecutionId id,
        Guid tenantId,
        Guid requestedByUserId,
        string queryText,
        string? domainFilter,
        SearchExecutionStatus status,
        int? resultCount,
        long? latencyMs,
        string? failureReason,
        DateTimeOffset requestedAtUtc,
        DateTimeOffset? completedAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        RequestedByUserId = requestedByUserId;
        QueryText = queryText;
        DomainFilter = domainFilter;
        Status = status;
        ResultCount = resultCount;
        LatencyMs = latencyMs;
        FailureReason = failureReason;
        RequestedAtUtc = requestedAtUtc;
        CompletedAtUtc = completedAtUtc;
    }

    /// <summary>
    /// Begins a new search execution's own lifecycle. <paramref name="tenantId"/> is
    /// guarded rather than Result-validated, the identical technical-precondition
    /// choice <see cref="IndexedDocument.Index"/>'s own remarks explain for the same
    /// reason (<c>CTR-ISO-001</c>).
    /// </summary>
    public static Result<SearchExecution> Request(
        Guid tenantId, Guid requestedByUserId, string? queryText, string? domainFilter, DateTimeOffset nowUtc)
    {
        Guard.AgainstDefault(tenantId, nameof(tenantId));

        if (string.IsNullOrWhiteSpace(queryText))
        {
            return Result.Failure<SearchExecution>(SearchErrors.QueryTextRequired);
        }

        var execution = new SearchExecution(
            new SearchExecutionId(Guid.NewGuid()), tenantId, requestedByUserId, queryText.Trim(), domainFilter?.Trim(), nowUtc);

        execution.AddDomainEvent(new SearchRequested(Guid.NewGuid(), nowUtc, execution.Id, tenantId, execution.QueryText));
        return Result.Success(execution);
    }

    public Result Complete(int resultCount, long latencyMs, DateTimeOffset nowUtc)
    {
        if (Status != SearchExecutionStatus.Requested)
        {
            return Result.Failure(SearchErrors.InvalidSearchExecutionTransition);
        }

        Status = SearchExecutionStatus.Completed;
        ResultCount = resultCount;
        LatencyMs = latencyMs;
        CompletedAtUtc = nowUtc;

        AddDomainEvent(new SearchCompleted(Guid.NewGuid(), nowUtc, Id, resultCount, latencyMs));
        return Result.Success();
    }

    public Result Fail(string? reason, DateTimeOffset nowUtc)
    {
        if (Status != SearchExecutionStatus.Requested)
        {
            return Result.Failure(SearchErrors.InvalidSearchExecutionTransition);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(SearchErrors.FailureReasonRequired);
        }

        Status = SearchExecutionStatus.Failed;
        FailureReason = reason.Trim();
        CompletedAtUtc = nowUtc;

        AddDomainEvent(new SearchFailed(Guid.NewGuid(), nowUtc, Id, FailureReason));
        return Result.Success();
    }
}
