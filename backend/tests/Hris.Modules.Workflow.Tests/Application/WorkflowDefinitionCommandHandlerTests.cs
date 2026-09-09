using FluentAssertions;
using Hris.Modules.Workflow.Application.Commands;
using Hris.Modules.Workflow.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Workflow.Tests.Application;

public sealed class AuthorWorkflowDefinitionCommandHandlerTests
{
    private readonly IWorkflowDefinitionRepository _repository = Substitute.For<IWorkflowDefinitionRepository>();
    private readonly AuthorWorkflowDefinitionCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public AuthorWorkflowDefinitionCommandHandlerTests()
    {
        _handler = new AuthorWorkflowDefinitionCommandHandler(_repository, new FakeTimeProvider(TestWorkflow.NowUtc));
    }

    [Fact]
    public async Task Handle_AddsTheDraft_WhenTheNameIsFree()
    {
        _repository.NameExistsForBusinessProcessAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<WorkflowDefinitionId?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _handler.Handle(
            new AuthorWorkflowDefinitionCommand(_tenantId, Guid.NewGuid(), "Leave Approval", null, null, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<WorkflowDefinition>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenTheNameAlreadyExistsForTheBusinessProcess()
    {
        _repository.NameExistsForBusinessProcessAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<WorkflowDefinitionId?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _handler.Handle(
            new AuthorWorkflowDefinitionCommand(_tenantId, Guid.NewGuid(), "Leave Approval", null, null, Guid.NewGuid()),
            CancellationToken.None);

        result.Error.Should().Be(WorkflowErrors.DefinitionNameNotUniqueForBusinessProcess);
        await _repository.DidNotReceive().AddAsync(Arg.Any<WorkflowDefinition>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenTheNameIsBlank_WithoutQueryingForCollision()
    {
        var result = await _handler.Handle(
            new AuthorWorkflowDefinitionCommand(_tenantId, Guid.NewGuid(), "  ", null, null, Guid.NewGuid()),
            CancellationToken.None);

        result.Error.Should().Be(WorkflowErrors.DefinitionNameRequired);
    }
}

public sealed class EditDraftDefinitionCommandHandlerTests
{
    private readonly IWorkflowDefinitionRepository _repository = Substitute.For<IWorkflowDefinitionRepository>();
    private readonly EditDraftDefinitionCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public EditDraftDefinitionCommandHandlerTests()
    {
        _handler = new EditDraftDefinitionCommandHandler(_repository);
    }

    private static WorkflowStepInput TerminalInput(Guid id, int order) => new(
        id, order, StepType.Terminal, $"End {order}", null, null, null, null, null, null, null, null, null, null, false,
        null, null, null, null, null, null);

    [Fact]
    public async Task Handle_ReplacesTheStepSet_OnADraft()
    {
        var definition = TestWorkflow.Draft(_tenantId);
        _repository.GetByIdAsync(definition.Id, Arg.Any<CancellationToken>()).Returns(definition);

        var result = await _handler.Handle(
            new EditDraftDefinitionCommand(_tenantId, definition.Id.Value, [TerminalInput(Guid.NewGuid(), 1)], Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        definition.Steps.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_Fails_WhenTheDefinitionIsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<WorkflowDefinitionId>(), Arg.Any<CancellationToken>()).Returns((WorkflowDefinition?)null);

        var result = await _handler.Handle(
            new EditDraftDefinitionCommand(_tenantId, Guid.NewGuid(), [], Guid.NewGuid()), CancellationToken.None);

        result.Error.Should().Be(WorkflowErrors.DefinitionNotFound);
    }

    /// <summary>CTR-ISO-002: an out-of-tenant identifier is not-found, never forbidden.</summary>
    [Fact]
    public async Task Handle_ReturnsNotFound_ForADefinitionInAnotherTenant()
    {
        var definition = TestWorkflow.Draft(Guid.NewGuid());
        _repository.GetByIdAsync(definition.Id, Arg.Any<CancellationToken>()).Returns(definition);

        var result = await _handler.Handle(
            new EditDraftDefinitionCommand(_tenantId, definition.Id.Value, [], Guid.NewGuid()), CancellationToken.None);

        result.Error.Should().Be(WorkflowErrors.DefinitionNotFound);
    }

    [Fact]
    public async Task Handle_Fails_WhenAStepInputIsInvalid()
    {
        var definition = TestWorkflow.Draft(_tenantId);
        _repository.GetByIdAsync(definition.Id, Arg.Any<CancellationToken>()).Returns(definition);

        var invalid = new WorkflowStepInput(
            Guid.NewGuid(), 1, StepType.Approval, "Approve", null, null, null, null, null, null, null, null, null, null,
            false, null, null, null, null, null, null);

        var result = await _handler.Handle(
            new EditDraftDefinitionCommand(_tenantId, definition.Id.Value, [invalid], Guid.NewGuid()), CancellationToken.None);

        result.Error.Should().Be(WorkflowErrors.ApprovalStepRequiresResolutionRule);
    }

    [Fact]
    public async Task Handle_Fails_AgainstAPublishedDefinition()
    {
        var definition = TestWorkflow.Published(_tenantId);
        _repository.GetByIdAsync(definition.Id, Arg.Any<CancellationToken>()).Returns(definition);

        var result = await _handler.Handle(
            new EditDraftDefinitionCommand(_tenantId, definition.Id.Value, [TerminalInput(Guid.NewGuid(), 1)], Guid.NewGuid()),
            CancellationToken.None);

        result.Error.Should().Be(WorkflowErrors.DefinitionNotDraft);
    }
}

public sealed class PublishWorkflowDefinitionCommandHandlerTests
{
    private readonly IWorkflowDefinitionRepository _repository = Substitute.For<IWorkflowDefinitionRepository>();
    private readonly PublishWorkflowDefinitionCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public PublishWorkflowDefinitionCommandHandlerTests()
    {
        _handler = new PublishWorkflowDefinitionCommandHandler(_repository, new FakeTimeProvider(TestWorkflow.NowUtc));
    }

    [Fact]
    public async Task Handle_Publishes_WhenEveryCheckPasses()
    {
        var definition = TestWorkflow.PublishableDraft(_tenantId, out _);
        _repository.GetByIdAsync(definition.Id, Arg.Any<CancellationToken>()).Returns(definition);

        var result = await _handler.Handle(
            new PublishWorkflowDefinitionCommand(_tenantId, definition.Id.Value, false, false, false, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        definition.Status.Should().Be(DefinitionStatus.Published);
    }

    [Fact]
    public async Task Handle_Fails_WhenTheDefinitionCouldRouteToTheRequester()
    {
        var definition = TestWorkflow.PublishableDraft(_tenantId, out _);
        _repository.GetByIdAsync(definition.Id, Arg.Any<CancellationToken>()).Returns(definition);

        var result = await _handler.Handle(
            new PublishWorkflowDefinitionCommand(_tenantId, definition.Id.Value, false, true, false, Guid.NewGuid()),
            CancellationToken.None);

        result.Error.Should().Be(WorkflowErrors.DefinitionCanRouteToRequester);
    }

    [Fact]
    public async Task Handle_Fails_WhenTheDefinitionIsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<WorkflowDefinitionId>(), Arg.Any<CancellationToken>()).Returns((WorkflowDefinition?)null);

        var result = await _handler.Handle(
            new PublishWorkflowDefinitionCommand(_tenantId, Guid.NewGuid(), false, false, false, Guid.NewGuid()),
            CancellationToken.None);

        result.Error.Should().Be(WorkflowErrors.DefinitionNotFound);
    }
}

public sealed class CreateNewVersionCommandHandlerTests
{
    private readonly IWorkflowDefinitionRepository _repository = Substitute.For<IWorkflowDefinitionRepository>();
    private readonly CreateNewVersionCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public CreateNewVersionCommandHandlerTests()
    {
        _handler = new CreateNewVersionCommandHandler(_repository, new FakeTimeProvider(TestWorkflow.NowUtc));
    }

    [Fact]
    public async Task Handle_AddsTheNewVersion_AsASeparateAggregate()
    {
        var published = TestWorkflow.Published(_tenantId);
        _repository.GetByIdAsync(published.Id, Arg.Any<CancellationToken>()).Returns(published);

        var result = await _handler.Handle(
            new CreateNewVersionCommand(_tenantId, published.Id.Value, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(published.Id.Value);
        await _repository.Received(1).AddAsync(
            Arg.Is<WorkflowDefinition>(d => d.Version == 2 && d.LineageId == published.LineageId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_AgainstADraft()
    {
        var draft = TestWorkflow.Draft(_tenantId);
        _repository.GetByIdAsync(draft.Id, Arg.Any<CancellationToken>()).Returns(draft);

        var result = await _handler.Handle(
            new CreateNewVersionCommand(_tenantId, draft.Id.Value, Guid.NewGuid()), CancellationToken.None);

        result.Error.Should().Be(WorkflowErrors.DefinitionNotPublished);
    }

    [Fact]
    public async Task Handle_Fails_WhenTheDefinitionIsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<WorkflowDefinitionId>(), Arg.Any<CancellationToken>()).Returns((WorkflowDefinition?)null);

        var result = await _handler.Handle(
            new CreateNewVersionCommand(_tenantId, Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.Error.Should().Be(WorkflowErrors.DefinitionNotFound);
    }
}

public sealed class DeprecateWorkflowDefinitionCommandHandlerTests
{
    private readonly IWorkflowDefinitionRepository _repository = Substitute.For<IWorkflowDefinitionRepository>();
    private readonly DeprecateWorkflowDefinitionCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public DeprecateWorkflowDefinitionCommandHandlerTests()
    {
        _handler = new DeprecateWorkflowDefinitionCommandHandler(_repository, new FakeTimeProvider(TestWorkflow.NowUtc));
    }

    [Fact]
    public async Task Handle_Deprecates_APublishedDefinition()
    {
        var definition = TestWorkflow.Published(_tenantId);
        _repository.GetByIdAsync(definition.Id, Arg.Any<CancellationToken>()).Returns(definition);

        var result = await _handler.Handle(
            new DeprecateWorkflowDefinitionCommand(_tenantId, definition.Id.Value, Guid.NewGuid(), "Superseded"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        definition.Status.Should().Be(DefinitionStatus.Deprecated);
    }

    [Fact]
    public async Task Handle_Fails_WhenTheDefinitionIsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<WorkflowDefinitionId>(), Arg.Any<CancellationToken>()).Returns((WorkflowDefinition?)null);

        var result = await _handler.Handle(
            new DeprecateWorkflowDefinitionCommand(_tenantId, Guid.NewGuid(), Guid.NewGuid(), "Reason"), CancellationToken.None);

        result.Error.Should().Be(WorkflowErrors.DefinitionNotFound);
    }
}
