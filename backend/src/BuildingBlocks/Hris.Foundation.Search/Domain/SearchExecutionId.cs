using Hris.SharedKernel;

namespace Hris.Foundation.Search.Domain;

/// <summary>
/// Identity of the <see cref="SearchExecution"/> Aggregate Root -- one per search
/// request/response cycle. Exists so search-framework.md's own <c>SearchRequested</c>/
/// <c>SearchCompleted</c>/<c>SearchFailed</c> Domain Events have a real aggregate to be
/// raised from, and so the framework's own Search Analytics bullets (Search Frequency,
/// Failed Searches, Search Latency) have real rows to compute from -- without this
/// framework itself building the Reporting/Business Intelligence layer its own Scope
/// section explicitly excludes.
/// </summary>
public readonly record struct SearchExecutionId(Guid Value) : IStronglyTypedId;
