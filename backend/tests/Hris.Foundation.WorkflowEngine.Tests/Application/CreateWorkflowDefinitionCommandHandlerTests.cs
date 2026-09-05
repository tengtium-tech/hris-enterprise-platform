using FluentAssertions;
using Hris.Foundation.WorkflowEngine.Application.Commands;
using Hris.Foundation.WorkflowEngine.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.WorkflowEngine.Tests.Application;

public sealed class CreateWorkflowDefinitionCommandHandlerTests
{
    private readonly IWorkflowDefinitionRepository _repository = Substitute.For<IWorkflowDefinitionRepository>();
    private readonly CreateWorkflowDefinitionCommandHandler _handler;

    public CreateWorkflowDefinitionCommandHandlerTests()
    {
        _handler = new CreateWorkflowDefinitionCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    private static CreateWorkflowDefinitionCommand ValidCommand() => new(
        TestData.TenantId, "Leave Approval", WorkflowTriggerType.SystemEvent, "leave.requested", TestData.NewSteps());

    [Fact]
    public async Task Handle_Succeeds_AndPersistsTheNewDefinition()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<WorkflowDefinition>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenNameIsMissing_WithoutCallingTheRepository()
    {
        var result = await _handler.Handle(ValidCommand() with { Name = string.Empty }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.DefinitionNameRequired);
        await _repository.DidNotReceive().AddAsync(Arg.Any<WorkflowDefinition>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenStepsAreEmpty()
    {
        var result = await _handler.Handle(ValidCommand() with { Steps = [] }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.StepsRequired);
    }
}
