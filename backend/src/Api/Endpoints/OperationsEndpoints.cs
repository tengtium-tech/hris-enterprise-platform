using Hris.Api.Http;
using Hris.Foundation.JobProcessing.Application.Dtos;
using Hris.Foundation.JobProcessing.Application.Queries;
using MediatR;

namespace Hris.Api.Endpoints;

/// <summary>
/// api-standards.md's own Long-Running Operations section, the first concrete
/// implementation of ADR-0006 Rule 6's accepted-response pattern: a client polls
/// <c>GET /api/v1/operations/{operationId}</c> for the status of an operation
/// previously accepted with <c>202</c> and a <c>Location</c> header pointing here.
///
/// Deliberately not a new Aggregate or persisted state of its own -- HEP-85's own
/// Jira description states this Sprint is "not a Foundation framework with its own
/// aggregate," and job-processing.md's own Overview already states the substrate
/// this pattern runs on: "Business modules should submit background work to the Job
/// Processing Framework instead of executing long-running processes within user
/// requests." This endpoint is a thin translation over that framework's own,
/// already-existing <see cref="GetJobQuery"/> -- exactly the "translate an HTTP
/// request into a query, dispatch through MediatR, and translate the result into an
/// HTTP response" a thin Controller (coding-standards.md's own Presentation Layer
/// convention) is supposed to do, one operation id at a time rather than a whole new
/// bounded context.
///
/// <see cref="OperationStatusDto.Progress"/> and <see cref="OperationStatusDto.ResultLink"/>
/// are always <c>null</c> in this Sprint's own build: <c>Job</c> (Sprint 4) tracks
/// neither a processed/total counter nor a result reference today. This is a stated
/// gap, not a silent omission -- a future job type that needs to report progress
/// extends <c>Job</c> itself first; this translation layer would then stop hard-coding
/// <c>null</c> for that one field, nothing else about this endpoint's own shape
/// would change.
/// </summary>
internal static class OperationsEndpoints
{
    public static IEndpointRouteBuilder MapOperationsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api/v1/operations").WithTags("Operations");

        group.MapGet("/{operationId:guid}", GetOperationStatusAsync)
            .WithName("GetOperationStatus")
            // api-standards.md's Rate Limiting section names "lookup-by-identifier" as
            // one of the endpoint classes this platform's own rate-limit policy
            // applies to -- this endpoint is exactly that class.
            .RequireRateLimiting(RateLimitPolicies.LookupByIdentifier);

        return endpoints;
    }

    private static async Task<IResult> GetOperationStatusAsync(
        Guid operationId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetJobQuery(operationId), cancellationToken).ConfigureAwait(false);

        return result.ToHttpResult(httpContext, job => Results.Ok(ToOperationStatusDto(job)));
    }

    private static OperationStatusDto ToOperationStatusDto(JobDto job) => new(
        job.JobId,
        MapStatus(job.Status),
        Progress: null,
        ResultLink: null);

    /// <summary>
    /// job-processing.md's own eight-value <c>JobStatus</c> collapses onto the
    /// three-value public shape api-standards.md's own worked example shows
    /// (<c>InProgress</c>/<c>Completed</c>/<c>Failed</c>) -- a caller polling this
    /// endpoint does not need to distinguish <c>Queued</c> from <c>Running</c>, only
    /// whether the operation has reached a terminal state yet.
    /// </summary>
    private static string MapStatus(string jobStatus) => jobStatus switch
    {
        "Completed" => "Completed",
        "Failed" or "DeadLetter" or "Cancelled" => "Failed",
        _ => "InProgress",
    };
}

internal sealed record OperationStatusDto(Guid OperationId, string Status, OperationProgressDto? Progress, string? ResultLink);

internal sealed record OperationProgressDto(int Processed, int Total);
