using Hris.Foundation.JobProcessing.Domain;

namespace Hris.Foundation.JobProcessing.Tests;

/// <summary>
/// Valid-default builders per docs/09-testing/unit-and-integration-testing.md 2.4:
/// "Construct aggregates through builders that supply valid defaults, so each test
/// specifies only the values relevant to what it verifies." A fixed clock
/// (<see cref="NowUtc"/>), never <c>DateTimeOffset.UtcNow</c>, per that same document's
/// own 2.1 ("must not touch... a clock").
/// </summary>
internal static class TestData
{
    public static readonly DateTimeOffset NowUtc = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    public static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static readonly Guid UserId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static JobQueueName NewQueueName(string? value = null) => JobQueueName.Create(value ?? "PayrollQueue").Value;

    public static JobQueue RegisteredQueue(
        JobQueueName? name = null, int maxConcurrency = 5, int defaultMaxRetries = 3, long defaultRetryDelaySeconds = 60, DateTimeOffset? nowUtc = null) =>
        JobQueue.Register(name ?? NewQueueName(), maxConcurrency, defaultMaxRetries, defaultRetryDelaySeconds, nowUtc ?? NowUtc).Value;

    public static Job SubmittedJob(
        Guid? tenantId = null,
        string jobType = "PayrollCalculation",
        JobQueueId? jobQueueId = null,
        JobPriority priority = JobPriority.Normal,
        string? payloadReference = null,
        Guid? submittedByUserId = null,
        int maxRetries = 3,
        DateTimeOffset? nowUtc = null) =>
        Job.Submit(
            tenantId ?? TenantId,
            jobType,
            jobQueueId ?? new JobQueueId(Guid.NewGuid()),
            priority,
            payloadReference,
            submittedByUserId ?? UserId,
            maxRetries,
            nowUtc ?? NowUtc).Value;

    public static Job QueuedJob(Guid? tenantId = null, JobQueueId? jobQueueId = null, int maxRetries = 3, DateTimeOffset? nowUtc = null)
    {
        var job = SubmittedJob(tenantId, jobQueueId: jobQueueId, maxRetries: maxRetries, nowUtc: nowUtc);
        job.Enqueue(nowUtc ?? NowUtc);
        return job;
    }

    public static Job RunningJob(Guid? tenantId = null, JobQueueId? jobQueueId = null, int maxRetries = 3, DateTimeOffset? nowUtc = null)
    {
        var job = QueuedJob(tenantId, jobQueueId, maxRetries, nowUtc);
        job.Start(nowUtc ?? NowUtc);
        return job;
    }

    public static Job FailedJob(Guid? tenantId = null, JobQueueId? jobQueueId = null, int maxRetries = 3, DateTimeOffset? nowUtc = null)
    {
        var job = RunningJob(tenantId, jobQueueId, maxRetries, nowUtc);
        job.Fail("Simulated failure.", nowUtc ?? NowUtc);
        return job;
    }

    public static Worker RunningWorker(string instanceId = "worker-0001", DateTimeOffset? nowUtc = null) =>
        Worker.Start(instanceId, nowUtc ?? NowUtc).Value;
}
