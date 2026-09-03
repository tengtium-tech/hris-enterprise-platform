using FluentAssertions;
using Hris.Foundation.JobProcessing.Application.Commands;
using Hris.Foundation.JobProcessing.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.JobProcessing.Tests.Application;

public sealed class WorkerLifecycleCommandHandlerTests
{
    private readonly IWorkerRepository _repository = Substitute.For<IWorkerRepository>();

    [Fact]
    public async Task StartWorker_Succeeds_AndPersistsTheNewWorker()
    {
        var handler = new StartWorkerCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));

        var result = await handler.Handle(new StartWorkerCommand("worker-0001"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<Worker>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartWorker_Fails_WhenInstanceIdIsMissing()
    {
        var handler = new StartWorkerCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));

        var result = await handler.Handle(new StartWorkerCommand(string.Empty), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(JobProcessingErrors.WorkerInstanceIdRequired);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Worker>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StopWorker_Succeeds_WhenWorkerExists()
    {
        var worker = TestData.RunningWorker();
        _repository.GetByIdAsync(worker.Id, Arg.Any<CancellationToken>()).Returns(worker);
        var handler = new StopWorkerCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));

        var result = await handler.Handle(new StopWorkerCommand(worker.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        worker.Status.Should().Be(WorkerStatus.Stopped);
    }

    [Fact]
    public async Task StopWorker_Fails_WhenWorkerDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<WorkerId>(), Arg.Any<CancellationToken>()).Returns((Worker?)null);
        var handler = new StopWorkerCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));

        var result = await handler.Handle(new StopWorkerCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(JobProcessingErrors.WorkerNotFound);
    }
}
