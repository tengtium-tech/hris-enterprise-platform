using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Hris.Application.Abstractions;
using Hris.Foundation.JobProcessing.Domain;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Hris.Api.Tests;

/// <summary>
/// api-standards.md's own Long-Running Operations section, exercised end to end
/// against the real host and a real database: GetJobQuery's own JobStatus is fully
/// exercised by Hris.Foundation.JobProcessing.Tests already, so these tests confirm
/// only OperationsEndpoints' own job -- translating that already-verified query
/// result into this endpoint's own public status shape and RFC 7807 not-found
/// response -- not re-deriving every JobStatus transition again.
/// </summary>
public sealed class OperationsEndpointTests : IClassFixture<HrisApiFactory>
{
    private readonly HrisApiFactory _factory;
    private readonly HttpClient _client;

    public OperationsEndpointTests(HrisApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetOperationStatus_Returns404_WithRfc7807Shape_ForAnOperationThatDoesNotExist()
    {
        var response = await _client.GetAsync(new Uri($"/api/v1/operations/{Guid.NewGuid()}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        body.Should().NotBeNull();
        body!.Status.Should().Be(404);
        body.Code.Should().Be("JobProcessing.JobNotFound");
        body.CorrelationId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetOperationStatus_EchoesTheCallerSuppliedCorrelationId_OnANotFoundResponse()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"/api/v1/operations/{Guid.NewGuid()}", UriKind.Relative));
        request.Headers.Add("X-Correlation-Id", "test-correlation-id");

        var response = await _client.SendAsync(request);

        response.Headers.GetValues("X-Correlation-Id").Should().ContainSingle().Which.Should().Be("test-correlation-id");

        var body = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        body!.CorrelationId.Should().Be("test-correlation-id");
    }

    [Theory]
    [InlineData(JobStatus.Submitted, "InProgress")]
    [InlineData(JobStatus.Running, "InProgress")]
    [InlineData(JobStatus.Completed, "Completed")]
    [InlineData(JobStatus.Failed, "Failed")]
    [InlineData(JobStatus.Cancelled, "Failed")]
    public async Task GetOperationStatus_MapsJobStatus_ToThePublicThreeValueShape(JobStatus jobStatus, string expectedStatus)
    {
        var jobId = await SeedJobAsync(jobStatus);

        var response = await _client.GetAsync(new Uri($"/api/v1/operations/{jobId}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<OperationStatusBody>();
        body.Should().NotBeNull();
        body!.OperationId.Should().Be(jobId);
        body.Status.Should().Be(expectedStatus);
        body.Progress.Should().BeNull();
        body.ResultLink.Should().BeNull();
    }

    private async Task<Guid> SeedJobAsync(JobStatus targetStatus)
    {
        using var scope = _factory.Services.CreateScope();
        var jobQueueRepository = scope.ServiceProvider.GetRequiredService<IJobQueueRepository>();
        var jobRepository = scope.ServiceProvider.GetRequiredService<IJobRepository>();
        var now = DateTimeOffset.UtcNow;

        var queueName = JobQueueName.Create($"operations-test-{Guid.NewGuid():N}").Value;
        var queue = JobQueue.Register(queueName, maxConcurrency: 1, defaultMaxRetries: 3, defaultRetryDelaySeconds: 30, now).Value;
        await jobQueueRepository.AddAsync(queue, CancellationToken.None);

        var job = Job.Submit(
            tenantId: Guid.NewGuid(),
            jobType: "operations-test",
            jobQueueId: queue.Id,
            priority: JobPriority.Normal,
            payloadReference: null,
            submittedByUserId: null,
            maxRetries: 3,
            nowUtc: now).Value;

        // Job.Start requires Queued or Scheduled, never Submitted directly -- Enqueue
        // is the real Submitted -> Queued transition every non-Submitted case below
        // needs first.
        switch (targetStatus)
        {
            case JobStatus.Running:
                job.Enqueue(now);
                job.Start(now);
                break;
            case JobStatus.Completed:
                job.Enqueue(now);
                job.Start(now);
                job.Complete(now);
                break;
            case JobStatus.Failed:
                job.Enqueue(now);
                job.Start(now);
                job.Fail("test failure", now);
                break;
            case JobStatus.Cancelled:
                job.Cancel(now);
                break;
        }

        await jobRepository.AddAsync(job, CancellationToken.None);

        // IJobRepository.AddAsync only stages the entity, per its own remarks --
        // SaveChangesAsync is TransactionBehavior's own job for every real command,
        // which this seeding bypasses entirely by calling the repository directly.
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        return job.Id.Value;
    }

    private sealed record ProblemDetailsBody(int Status, string Code, string CorrelationId);

    private sealed record OperationStatusBody(Guid OperationId, string Status, object? Progress, string? ResultLink);
}
