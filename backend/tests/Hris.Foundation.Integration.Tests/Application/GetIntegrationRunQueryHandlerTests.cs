using FluentAssertions;
using Hris.Foundation.Integration.Application.Queries;
using Hris.Foundation.Integration.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Integration.Tests.Application;

public sealed class GetIntegrationRunQueryHandlerTests
{
    private readonly IIntegrationRunRepository _repository = Substitute.For<IIntegrationRunRepository>();
    private readonly GetIntegrationRunQueryHandler _handler;

    public GetIntegrationRunQueryHandlerTests()
    {
        _handler = new GetIntegrationRunQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_Succeeds_WhenRunExistsForTheSameTenant()
    {
        var jobId = Guid.NewGuid();
        var run = IntegrationRun.Start(
            TestData.TenantId, new ConnectorId(Guid.NewGuid()), IntegrationRunKind.Synchronization, "Incremental", jobId,
            maxRetries: 3, TestData.NowUtc).Value;
        run.Complete(25, TestData.NowUtc);
        _repository.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);

        var result = await _handler.Handle(new GetIntegrationRunQuery(run.Id.Value, TestData.TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.IntegrationRunId.Should().Be(run.Id.Value);
        dto.TenantId.Should().Be(run.TenantId);
        dto.ConnectorId.Should().Be(run.ConnectorId.Value);
        dto.RunKind.Should().Be(run.RunKind.ToString());
        dto.SyncModel.Should().Be("Incremental");
        dto.JobId.Should().Be(jobId);
        dto.Status.Should().Be(run.Status.ToString());
        dto.StartedAtUtc.Should().Be(run.StartedAtUtc);
        dto.CompletedAtUtc.Should().Be(TestData.NowUtc);
        dto.RecordsProcessed.Should().Be(25);
        dto.RetryCount.Should().Be(0);
        dto.MaxRetries.Should().Be(run.MaxRetries);
    }

    [Fact]
    public async Task Handle_Succeeds_AndCarriesTheFailureReason_ForAFailedRun()
    {
        var run = TestData.FailedRun();
        _repository.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);

        var result = await _handler.Handle(new GetIntegrationRunQuery(run.Id.Value, TestData.TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FailureReason.Should().Be(run.FailureReason);
    }

    [Fact]
    public async Task Handle_Fails_WhenRunDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<IntegrationRunId>(), Arg.Any<CancellationToken>()).Returns((IntegrationRun?)null);

        var result = await _handler.Handle(new GetIntegrationRunQuery(Guid.NewGuid(), TestData.TenantId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.IntegrationRunNotFound);
    }
}
