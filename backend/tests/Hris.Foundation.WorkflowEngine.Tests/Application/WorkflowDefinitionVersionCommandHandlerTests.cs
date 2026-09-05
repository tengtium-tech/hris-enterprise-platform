using FluentAssertions;
using Hris.Foundation.WorkflowEngine.Application.Commands;
using Hris.Foundation.WorkflowEngine.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.WorkflowEngine.Tests.Application;

public sealed class WorkflowDefinitionVersionCommandHandlerTests
{
    private readonly IWorkflowDefinitionRepository _repository = Substitute.For<IWorkflowDefinitionRepository>();
    private readonly FakeTimeProvider _timeProvider = new(TestData.NowUtc);

    [Fact]
    public async Task CreateNewDraftVersion_Succeeds_WhenDefinitionExists()
    {
        var definition = TestData.PublishedDefinition();
        _repository.GetByIdAsync(definition.Id, Arg.Any<CancellationToken>()).Returns(definition);
        var handler = new CreateNewWorkflowDefinitionDraftVersionCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(
            new CreateNewWorkflowDefinitionDraftVersionCommand(definition.Id.Value, TestData.NewSteps()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);
    }

    [Fact]
    public async Task CreateNewDraftVersion_Fails_WhenDefinitionDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<WorkflowDefinitionId>(), Arg.Any<CancellationToken>()).Returns((WorkflowDefinition?)null);
        var handler = new CreateNewWorkflowDefinitionDraftVersionCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(
            new CreateNewWorkflowDefinitionDraftVersionCommand(Guid.NewGuid(), TestData.NewSteps()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.DefinitionNotFound);
    }

    [Fact]
    public async Task PublishVersion_Succeeds_WhenDefinitionAndVersionExist()
    {
        var definition = TestData.NewDefinition();
        _repository.GetByIdAsync(definition.Id, Arg.Any<CancellationToken>()).Returns(definition);
        var handler = new PublishWorkflowDefinitionVersionCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new PublishWorkflowDefinitionVersionCommand(definition.Id.Value, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        definition.Versions[0].Status.Should().Be(WorkflowDefinitionVersionStatus.Published);
    }

    [Fact]
    public async Task PublishVersion_Fails_WhenDefinitionDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<WorkflowDefinitionId>(), Arg.Any<CancellationToken>()).Returns((WorkflowDefinition?)null);
        var handler = new PublishWorkflowDefinitionVersionCommandHandler(_repository, _timeProvider);

        var result = await handler.Handle(new PublishWorkflowDefinitionVersionCommand(Guid.NewGuid(), 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.DefinitionNotFound);
    }

    [Fact]
    public async Task DeprecateVersion_Succeeds_WhenDefinitionAndVersionExist()
    {
        var definition = TestData.PublishedDefinition();
        _repository.GetByIdAsync(definition.Id, Arg.Any<CancellationToken>()).Returns(definition);
        var handler = new DeprecateWorkflowDefinitionVersionCommandHandler(_repository);

        var result = await handler.Handle(new DeprecateWorkflowDefinitionVersionCommand(definition.Id.Value, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        definition.Versions[0].Status.Should().Be(WorkflowDefinitionVersionStatus.Deprecated);
    }

    [Fact]
    public async Task DeprecateVersion_Fails_WhenDefinitionDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<WorkflowDefinitionId>(), Arg.Any<CancellationToken>()).Returns((WorkflowDefinition?)null);
        var handler = new DeprecateWorkflowDefinitionVersionCommandHandler(_repository);

        var result = await handler.Handle(new DeprecateWorkflowDefinitionVersionCommand(Guid.NewGuid(), 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.DefinitionNotFound);
    }
}
