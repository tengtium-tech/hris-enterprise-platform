using FluentAssertions;
using Hris.Modules.Workflow.Domain;
using Xunit;

namespace Hris.Modules.Workflow.Tests.Domain;

public sealed class WorkflowDefinitionTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Author_CreatesADraft_SharingItsOwnIdentifierAsLineage()
    {
        var id = new WorkflowDefinitionId(Guid.NewGuid());

        var result = WorkflowDefinition.Author(
            id, _tenantId, Guid.NewGuid(), "Leave Approval", "desc", "days > 5", false, Guid.NewGuid(), TestWorkflow.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(DefinitionStatus.Draft);
        result.Value.Version.Should().Be(1);
        result.Value.LineageId.Should().Be(id.Value);
        result.Value.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<WorkflowDefinitionCreated>();
    }

    [Fact]
    public void Author_Fails_WhenNameIsMissing()
    {
        var result = WorkflowDefinition.Author(
            new WorkflowDefinitionId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), "  ", null, null, false, Guid.NewGuid(),
            TestWorkflow.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowErrors.DefinitionNameRequired);
    }

    [Fact]
    public void Author_Fails_WhenNameCollidesWithinTheBusinessProcess()
    {
        var result = WorkflowDefinition.Author(
            new WorkflowDefinitionId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), "Leave Approval", null, null, true,
            Guid.NewGuid(), TestWorkflow.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowErrors.DefinitionNameNotUniqueForBusinessProcess);
    }

    [Fact]
    public void Publish_Succeeds_ForTheSmallestValidGraph()
    {
        var definition = TestWorkflow.PublishableDraft(_tenantId, out _);

        var result = definition.Publish(false, false, false, Guid.NewGuid(), TestWorkflow.NowUtc);

        result.IsSuccess.Should().BeTrue();
        definition.Status.Should().Be(DefinitionStatus.Published);
        definition.PublishedOn.Should().Be(TestWorkflow.NowUtc);
        definition.DomainEvents.OfType<WorkflowDefinitionPublished>().Should().ContainSingle()
            .Which.StepCount.Should().Be(2);
    }

    [Fact]
    public void Publish_Fails_WhenTheDefinitionHasNoSteps()
    {
        var definition = TestWorkflow.Draft(_tenantId);

        var result = definition.Publish(false, false, false, Guid.NewGuid(), TestWorkflow.NowUtc);

        result.Error.Should().Be(WorkflowErrors.DefinitionHasNoSteps);
        definition.Status.Should().Be(DefinitionStatus.Draft, "a failed publication leaves the definition a draft");
    }

    [Fact]
    public void Publish_Fails_WhenTheGraphContainsACycle()
    {
        var definition = TestWorkflow.Draft(_tenantId);
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        definition.ReplaceSteps([TestWorkflow.ApprovalStep(first, 1, second), TestWorkflow.ApprovalStep(second, 2, first)]);

        var result = definition.Publish(false, false, false, Guid.NewGuid(), TestWorkflow.NowUtc);

        result.Error.Should().Be(WorkflowErrors.StepGraphContainsCycle);
    }

    [Fact]
    public void Publish_Fails_WhenAStepIsUnreachable()
    {
        var definition = TestWorkflow.Draft(_tenantId);
        var entry = Guid.NewGuid();
        var terminal = Guid.NewGuid();
        var orphanTerminal = Guid.NewGuid();

        definition.ReplaceSteps([
            TestWorkflow.ApprovalStep(entry, 1, terminal),
            TestWorkflow.TerminalStep(terminal, 2),
            TestWorkflow.TerminalStep(orphanTerminal, 3),
        ]);

        var result = definition.Publish(false, false, false, Guid.NewGuid(), TestWorkflow.NowUtc);

        result.Error.Should().Be(WorkflowErrors.StepGraphContainsUnreachableStep);
    }

    [Fact]
    public void Publish_Fails_WhenANonTerminalStepHasNoSuccessor()
    {
        var definition = TestWorkflow.Draft(_tenantId);
        definition.ReplaceSteps([TestWorkflow.ApprovalStep(Guid.NewGuid(), 1, null)]);

        var result = definition.Publish(false, false, false, Guid.NewGuid(), TestWorkflow.NowUtc);

        result.Error.Should().Be(WorkflowErrors.StepGraphDoesNotTerminate);
    }

    [Fact]
    public void Publish_Fails_WhenAStepNamesASuccessorOutsideTheDefinition()
    {
        var definition = TestWorkflow.Draft(_tenantId);
        definition.ReplaceSteps([TestWorkflow.ApprovalStep(Guid.NewGuid(), 1, Guid.NewGuid())]);

        var result = definition.Publish(false, false, false, Guid.NewGuid(), TestWorkflow.NowUtc);

        result.Error.Should().Be(WorkflowErrors.StepSuccessorNotFound);
    }

    /// <summary>
    /// WR-011's publication half. The caller reports that some step could statically
    /// resolve to the requester, and publication rejects the definition outright,
    /// under every tenant configuration.
    /// </summary>
    [Fact]
    public void Publish_Fails_WhenAnyStepCouldRouteToTheRequester()
    {
        var definition = TestWorkflow.PublishableDraft(_tenantId, out _);

        var result = definition.Publish(false, true, false, Guid.NewGuid(), TestWorkflow.NowUtc);

        result.Error.Should().Be(WorkflowErrors.DefinitionCanRouteToRequester);
        definition.Status.Should().Be(DefinitionStatus.Draft);
    }

    [Fact]
    public void Publish_Fails_WhenAReferencedCommandDoesNotExist()
    {
        var definition = TestWorkflow.PublishableDraft(_tenantId, out _);

        var result = definition.Publish(true, false, false, Guid.NewGuid(), TestWorkflow.NowUtc);

        result.Error.Should().Be(WorkflowErrors.ReferencedCommandDoesNotExist);
    }

    [Fact]
    public void Publish_Fails_WhenTriggerConditionsOverlapAnotherPublishedDefinition()
    {
        var definition = TestWorkflow.PublishableDraft(_tenantId, out _);

        var result = definition.Publish(false, false, true, Guid.NewGuid(), TestWorkflow.NowUtc);

        result.Error.Should().Be(WorkflowErrors.TriggerConditionOverlapsPublishedDefinition);
    }

    [Fact]
    public void Publish_Fails_WhenAlreadyPublished_RatherThanSilentlySucceeding()
    {
        var definition = TestWorkflow.Published(_tenantId);

        var result = definition.Publish(false, false, false, Guid.NewGuid(), TestWorkflow.NowUtc);

        result.Error.Should().Be(WorkflowErrors.DefinitionAlreadyPublished);
    }

    [Fact]
    public void Publish_Fails_WhenTheDefinitionIsDeprecated()
    {
        var definition = TestWorkflow.Published(_tenantId);
        definition.Deprecate(Guid.NewGuid(), "Replaced", TestWorkflow.NowUtc);

        var result = definition.Publish(false, false, false, Guid.NewGuid(), TestWorkflow.NowUtc);

        result.Error.Should().Be(WorkflowErrors.DefinitionNotDraft);
    }

    [Fact]
    public void ReplaceSteps_Fails_OnceThePublishedDefinitionIsImmutable()
    {
        var definition = TestWorkflow.Published(_tenantId);

        var result = definition.ReplaceSteps([TestWorkflow.TerminalStep(Guid.NewGuid(), 1)]);

        result.Error.Should().Be(WorkflowErrors.DefinitionNotDraft);
    }

    [Fact]
    public void AddStep_Fails_AgainstAPublishedDefinition()
    {
        var definition = TestWorkflow.Published(_tenantId);

        var result = definition.AddStep(TestWorkflow.TerminalStep(Guid.NewGuid(), 9));

        result.Error.Should().Be(WorkflowErrors.DefinitionNotDraft);
    }

    [Fact]
    public void AddStep_Succeeds_AgainstADraft()
    {
        var definition = TestWorkflow.Draft(_tenantId);

        var result = definition.AddStep(TestWorkflow.TerminalStep(Guid.NewGuid(), 1));

        result.IsSuccess.Should().BeTrue();
        definition.Steps.Should().ContainSingle();
    }

    [Fact]
    public void RemoveStep_RemovesIt_FromADraft()
    {
        var definition = TestWorkflow.Draft(_tenantId);
        var stepId = Guid.NewGuid();
        definition.AddStep(TestWorkflow.TerminalStep(stepId, 1));

        var result = definition.RemoveStep(new WorkflowStepId(stepId));

        result.IsSuccess.Should().BeTrue();
        definition.Steps.Should().BeEmpty();
    }

    [Fact]
    public void RemoveStep_Fails_WhenTheStepIsNotPartOfTheDefinition()
    {
        var definition = TestWorkflow.Draft(_tenantId);

        var result = definition.RemoveStep(new WorkflowStepId(Guid.NewGuid()));

        result.Error.Should().Be(WorkflowErrors.StepNotFound);
    }

    [Fact]
    public void RemoveStep_Fails_AgainstAPublishedDefinition()
    {
        var definition = TestWorkflow.Published(_tenantId);

        var result = definition.RemoveStep(new WorkflowStepId(definition.Steps[0].Id.Value));

        result.Error.Should().Be(WorkflowErrors.DefinitionNotDraft);
    }

    [Fact]
    public void Rename_Succeeds_AgainstADraft()
    {
        var definition = TestWorkflow.Draft(_tenantId);

        var result = definition.Rename("Renamed", "new description", "amount > 10", false);

        result.IsSuccess.Should().BeTrue();
        definition.Name.Should().Be("Renamed");
        definition.Description.Should().Be("new description");
    }

    [Fact]
    public void Rename_Fails_AgainstAPublishedDefinition()
    {
        var definition = TestWorkflow.Published(_tenantId);

        var result = definition.Rename("Renamed", null, null, false);

        result.Error.Should().Be(WorkflowErrors.DefinitionNotDraft);
    }

    [Fact]
    public void Rename_Fails_WhenNameIsMissing()
    {
        var definition = TestWorkflow.Draft(_tenantId);

        var result = definition.Rename("   ", null, null, false);

        result.Error.Should().Be(WorkflowErrors.DefinitionNameRequired);
    }

    [Fact]
    public void Rename_Fails_WhenTheNewNameCollides()
    {
        var definition = TestWorkflow.Draft(_tenantId);

        var result = definition.Rename("Taken", null, null, true);

        result.Error.Should().Be(WorkflowErrors.DefinitionNameNotUniqueForBusinessProcess);
    }

    /// <summary>
    /// WR-050 and CTR-WFL-005: a new version is a new aggregate instance sharing
    /// lineage, and the previous version is left completely untouched so an instance
    /// bound to it is never edited out from under itself.
    /// </summary>
    [Fact]
    public void CreateNewVersion_ProducesANewDraftSharingLineage_LeavingThePreviousVersionUnchanged()
    {
        var published = TestWorkflow.Published(_tenantId);
        var newId = new WorkflowDefinitionId(Guid.NewGuid());

        var result = published.CreateNewVersion(newId, Guid.NewGuid(), TestWorkflow.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(newId);
        result.Value.LineageId.Should().Be(published.LineageId);
        result.Value.Version.Should().Be(published.Version + 1);
        result.Value.Status.Should().Be(DefinitionStatus.Draft);
        result.Value.Steps.Should().HaveCount(published.Steps.Count);

        published.Status.Should().Be(DefinitionStatus.Published);
        published.Version.Should().Be(1);
    }

    [Fact]
    public void CreateNewVersion_CarriesForwardNameAndBusinessProcess_ButNotPublicationHistory()
    {
        var published = TestWorkflow.Published(_tenantId);

        var next = published.CreateNewVersion(new WorkflowDefinitionId(Guid.NewGuid()), Guid.NewGuid(), TestWorkflow.NowUtc).Value;

        next.Name.Should().Be(published.Name);
        next.BusinessProcessId.Should().Be(published.BusinessProcessId);
        next.PublishedOn.Should().BeNull();
        next.PublishedBy.Should().BeNull();
    }

    [Fact]
    public void CreateNewVersion_RaisesVersionedEventOnTheNewVersion()
    {
        var published = TestWorkflow.Published(_tenantId);

        var next = published.CreateNewVersion(new WorkflowDefinitionId(Guid.NewGuid()), Guid.NewGuid(), TestWorkflow.NowUtc).Value;

        var versioned = next.DomainEvents.OfType<WorkflowDefinitionVersioned>().Should().ContainSingle().Subject;
        versioned.PreviousVersion.Should().Be(1);
        versioned.NewVersion.Should().Be(2);
    }

    [Fact]
    public void CreateNewVersion_Fails_AgainstADraft()
    {
        var draft = TestWorkflow.Draft(_tenantId);

        var result = draft.CreateNewVersion(new WorkflowDefinitionId(Guid.NewGuid()), Guid.NewGuid(), TestWorkflow.NowUtc);

        result.Error.Should().Be(WorkflowErrors.DefinitionNotPublished);
    }

    [Fact]
    public void Deprecate_WithdrawsFromNewInstances()
    {
        var definition = TestWorkflow.Published(_tenantId);

        var result = definition.Deprecate(Guid.NewGuid(), "Superseded", TestWorkflow.NowUtc);

        result.IsSuccess.Should().BeTrue();
        definition.Status.Should().Be(DefinitionStatus.Deprecated);
        definition.DeprecationReason.Should().Be("Superseded");
        definition.DomainEvents.OfType<WorkflowDefinitionDeprecated>().Should().ContainSingle();
    }

    [Fact]
    public void Deprecate_IsIdempotent()
    {
        var definition = TestWorkflow.Published(_tenantId);
        definition.Deprecate(Guid.NewGuid(), "Superseded", TestWorkflow.NowUtc);

        var result = definition.Deprecate(Guid.NewGuid(), "Again", TestWorkflow.NowUtc);

        result.IsSuccess.Should().BeTrue();
        definition.DomainEvents.OfType<WorkflowDefinitionDeprecated>().Should().ContainSingle(
            "an idempotent no-op raises no second event");
    }

    [Fact]
    public void Deprecate_Fails_AgainstADraft()
    {
        var draft = TestWorkflow.Draft(_tenantId);

        var result = draft.Deprecate(Guid.NewGuid(), "Reason", TestWorkflow.NowUtc);

        result.Error.Should().Be(WorkflowErrors.DefinitionNotPublished);
    }

    [Fact]
    public void ValidateGraph_Fails_WhenAConditionalStepLacksADefaultSuccessor()
    {
        var definition = TestWorkflow.Draft(_tenantId);
        var terminal = Guid.NewGuid();
        var conditionalId = Guid.NewGuid();

        // Constructed through the entity, which requires a default successor, then the
        // graph check is exercised against a step whose default target was removed from
        // the definition entirely -- publication's second line of defense.
        var conditional = TestWorkflow.ConditionalStep(conditionalId, 1, terminal, Guid.NewGuid());
        definition.ReplaceSteps([conditional, TestWorkflow.TerminalStep(terminal, 2)]);

        var result = definition.ValidateGraph();

        result.Error.Should().Be(WorkflowErrors.StepSuccessorNotFound);
    }

    [Fact]
    public void ValidateGraph_Succeeds_ForAConditionalGraphWhereEveryTargetResolves()
    {
        var definition = TestWorkflow.Draft(_tenantId);
        var branchTarget = Guid.NewGuid();
        var defaultTarget = Guid.NewGuid();
        var conditionalId = Guid.NewGuid();

        definition.ReplaceSteps([
            TestWorkflow.ConditionalStep(conditionalId, 1, branchTarget, defaultTarget),
            TestWorkflow.TerminalStep(branchTarget, 2),
            TestWorkflow.TerminalStep(defaultTarget, 3),
        ]);

        definition.ValidateGraph().IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateGraph_Succeeds_ForAParallelFanOutAndJoin()
    {
        var definition = TestWorkflow.Draft(_tenantId);
        var entry = Guid.NewGuid();
        var left = Guid.NewGuid();
        var right = Guid.NewGuid();
        var join = Guid.NewGuid();

        var fanOut = WorkflowStep.Create(
            new WorkflowStepId(entry), 1, StepType.Approval, "Fan out", TestWorkflow.ReportingLineRule, null, null,
            [left, right], null, null, false, null, null).Value;

        var joinStep = WorkflowStep.Create(
            new WorkflowStepId(join), 4, StepType.Terminal, "Join", null, null, null, null, null, null, false, null,
            null).Value;

        definition.ReplaceSteps([
            fanOut,
            TestWorkflow.ApprovalStep(left, 2, join),
            TestWorkflow.ApprovalStep(right, 3, join),
            joinStep,
        ]);

        definition.ValidateGraph().IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ReplaceSteps_Throws_WhenGivenNull()
    {
        var definition = TestWorkflow.Draft(_tenantId);

        var act = () => definition.ReplaceSteps(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddStep_Throws_WhenGivenNull()
    {
        var definition = TestWorkflow.Draft(_tenantId);

        var act = () => definition.AddStep(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
