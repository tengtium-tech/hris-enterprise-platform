using FluentAssertions;
using Hris.Foundation.Integration.Application.Commands;
using Hris.Foundation.Integration.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Integration.Tests.Application;

public sealed class CompleteIntegrationRunCommandHandlerTests
{
    private readonly IIntegrationRunRepository _repository = Substitute.For<IIntegrationRunRepository>();
    private readonly CompleteIntegrationRunCommandHandler _handler;

    public CompleteIntegrationRunCommandHandlerTests()
    {
        _handler = new CompleteIntegrationRunCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenStarted()
    {
        var run = TestData.StartedRun();
        _repository.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);

        var result = await _handler.Handle(
            new CompleteIntegrationRunCommand(run.Id.Value, TestData.TenantId, 10), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        run.Status.Should().Be(IntegrationRunStatus.Completed);
        run.RecordsProcessed.Should().Be(10);
    }

    [Fact]
    public async Task Handle_Fails_WhenRunDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<IntegrationRunId>(), Arg.Any<CancellationToken>()).Returns((IntegrationRun?)null);

        var result = await _handler.Handle(
            new CompleteIntegrationRunCommand(Guid.NewGuid(), TestData.TenantId, 10), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.IntegrationRunNotFound);
    }

    [Fact]
    public async Task Handle_Fails_WithIntegrationRunNotFound_WhenRunBelongsToAnotherTenant()
    {
        var run = TestData.StartedRun();
        _repository.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);

        var result = await _handler.Handle(
            new CompleteIntegrationRunCommand(run.Id.Value, Guid.NewGuid(), 10), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.IntegrationRunNotFound);
    }
}

public sealed class FailIntegrationRunCommandHandlerTests
{
    private readonly IIntegrationRunRepository _repository = Substitute.For<IIntegrationRunRepository>();
    private readonly FailIntegrationRunCommandHandler _handler;

    public FailIntegrationRunCommandHandlerTests()
    {
        _handler = new FailIntegrationRunCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenStarted()
    {
        var run = TestData.StartedRun();
        _repository.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);

        var result = await _handler.Handle(
            new FailIntegrationRunCommand(run.Id.Value, TestData.TenantId, "timeout"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        run.Status.Should().Be(IntegrationRunStatus.Failed);
    }

    [Fact]
    public async Task Handle_Fails_WhenRunDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<IntegrationRunId>(), Arg.Any<CancellationToken>()).Returns((IntegrationRun?)null);

        var result = await _handler.Handle(
            new FailIntegrationRunCommand(Guid.NewGuid(), TestData.TenantId, "timeout"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.IntegrationRunNotFound);
    }
}

public sealed class RetryIntegrationRunCommandHandlerTests
{
    private readonly IIntegrationRunRepository _repository = Substitute.For<IIntegrationRunRepository>();
    private readonly RetryIntegrationRunCommandHandler _handler;

    public RetryIntegrationRunCommandHandlerTests()
    {
        _handler = new RetryIntegrationRunCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenFailed()
    {
        var run = TestData.FailedRun();
        _repository.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);

        var result = await _handler.Handle(new RetryIntegrationRunCommand(run.Id.Value, TestData.TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        run.Status.Should().Be(IntegrationRunStatus.Started);
        run.RetryCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Fails_WhenRunDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<IntegrationRunId>(), Arg.Any<CancellationToken>()).Returns((IntegrationRun?)null);

        var result = await _handler.Handle(new RetryIntegrationRunCommand(Guid.NewGuid(), TestData.TenantId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.IntegrationRunNotFound);
    }
}

public sealed class MoveIntegrationRunToDeadLetterCommandHandlerTests
{
    private readonly IIntegrationRunRepository _repository = Substitute.For<IIntegrationRunRepository>();
    private readonly MoveIntegrationRunToDeadLetterCommandHandler _handler;

    public MoveIntegrationRunToDeadLetterCommandHandlerTests()
    {
        _handler = new MoveIntegrationRunToDeadLetterCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_Succeeds_WhenFailed()
    {
        var run = TestData.FailedRun();
        _repository.GetByIdAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);

        var result = await _handler.Handle(
            new MoveIntegrationRunToDeadLetterCommand(run.Id.Value, TestData.TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        run.Status.Should().Be(IntegrationRunStatus.DeadLetter);
    }

    [Fact]
    public async Task Handle_Fails_WhenRunDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<IntegrationRunId>(), Arg.Any<CancellationToken>()).Returns((IntegrationRun?)null);

        var result = await _handler.Handle(
            new MoveIntegrationRunToDeadLetterCommand(Guid.NewGuid(), TestData.TenantId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IntegrationErrors.IntegrationRunNotFound);
    }
}
