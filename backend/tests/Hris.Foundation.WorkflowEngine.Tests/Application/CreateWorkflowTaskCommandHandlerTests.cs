using FluentAssertions;
using Hris.Foundation.WorkflowEngine.Application.Commands;
using Hris.Foundation.WorkflowEngine.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.WorkflowEngine.Tests.Application;

public sealed class CreateWorkflowTaskCommandHandlerTests
{
    private readonly IWorkflowTaskRepository _repository = Substitute.For<IWorkflowTaskRepository>();
    private readonly CreateWorkflowTaskCommandHandler _handler;

    public CreateWorkflowTaskCommandHandlerTests()
    {
        _handler = new CreateWorkflowTaskCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_AndPersistsTheNewTask()
    {
        var result = await _handler.Handle(
            new CreateWorkflowTaskCommand(
                TestData.TenantId, Guid.NewGuid(), "Manager Approval", 1, WorkflowParticipantType.Role, "PeopleManager", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<WorkflowTask>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenStepNameIsMissing()
    {
        var result = await _handler.Handle(
            new CreateWorkflowTaskCommand(TestData.TenantId, Guid.NewGuid(), string.Empty, 1, WorkflowParticipantType.Role, "PeopleManager", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repository.DidNotReceive().AddAsync(Arg.Any<WorkflowTask>(), Arg.Any<CancellationToken>());
    }
}
