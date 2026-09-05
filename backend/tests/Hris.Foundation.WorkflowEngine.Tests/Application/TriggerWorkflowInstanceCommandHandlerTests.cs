using FluentAssertions;
using Hris.Foundation.WorkflowEngine.Application.Commands;
using Hris.Foundation.WorkflowEngine.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.WorkflowEngine.Tests.Application;

public sealed class TriggerWorkflowInstanceCommandHandlerTests
{
    private readonly IWorkflowDefinitionRepository _definitionRepository = Substitute.For<IWorkflowDefinitionRepository>();
    private readonly IWorkflowInstanceRepository _instanceRepository = Substitute.For<IWorkflowInstanceRepository>();
    private readonly TriggerWorkflowInstanceCommandHandler _handler;

    public TriggerWorkflowInstanceCommandHandlerTests()
    {
        _handler = new TriggerWorkflowInstanceCommandHandler(_definitionRepository, _instanceRepository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_AgainstThePublishedVersion_AndPersistsTheNewInstance()
    {
        var definition = TestData.PublishedDefinition();
        _definitionRepository.GetByIdAsync(definition.Id, Arg.Any<CancellationToken>()).Returns(definition);

        var result = await _handler.Handle(
            new TriggerWorkflowInstanceCommand(TestData.TenantId, definition.Id.Value, "leave-request-0001", TestData.InitiatorUserId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _instanceRepository.Received(1).AddAsync(
            Arg.Is<WorkflowInstance>(i => i.WorkflowDefinitionVersionNumber == 1), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenDefinitionDoesNotExist()
    {
        _definitionRepository.GetByIdAsync(Arg.Any<WorkflowDefinitionId>(), Arg.Any<CancellationToken>()).Returns((WorkflowDefinition?)null);

        var result = await _handler.Handle(
            new TriggerWorkflowInstanceCommand(TestData.TenantId, Guid.NewGuid(), null, TestData.InitiatorUserId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.DefinitionNotFound);
    }

    [Fact]
    public async Task Handle_Fails_WhenDefinitionHasNoPublishedVersion()
    {
        var definition = TestData.NewDefinition();
        _definitionRepository.GetByIdAsync(definition.Id, Arg.Any<CancellationToken>()).Returns(definition);

        var result = await _handler.Handle(
            new TriggerWorkflowInstanceCommand(TestData.TenantId, definition.Id.Value, null, TestData.InitiatorUserId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.VersionNotFound);
        await _instanceRepository.DidNotReceive().AddAsync(Arg.Any<WorkflowInstance>(), Arg.Any<CancellationToken>());
    }
}
