using FluentAssertions;
using Hris.Foundation.WorkflowEngine.Domain;
using Xunit;

namespace Hris.Foundation.WorkflowEngine.Tests.Domain;

public sealed class WorkflowDefinitionTests
{
    [Fact]
    public void Create_Succeeds_WithFirstDraftVersion_AndRaisesNoEvent()
    {
        var result = WorkflowDefinition.Create(
            TestData.TenantId, "Leave Approval", WorkflowTriggerType.SystemEvent, "leave.requested", TestData.NewSteps(), TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be(TestData.TenantId);
        result.Value.Name.Should().Be("Leave Approval");
        result.Value.Versions.Should().ContainSingle();
        result.Value.Versions[0].VersionNumber.Should().Be(1);
        result.Value.Versions[0].Status.Should().Be(WorkflowDefinitionVersionStatus.Draft);
        result.Value.DomainEvents.Should().BeEmpty("workflow-engine.md names no definition-registered event, the same asymmetry JobQueue.Register's own remarks state for itself");
    }

    [Fact]
    public void Create_Throws_WhenTenantIdIsEmpty()
    {
        var act = () => WorkflowDefinition.Create(
            Guid.Empty, "Leave Approval", WorkflowTriggerType.SystemEvent, "leave.requested", TestData.NewSteps(), TestData.NowUtc);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Fails_WhenNameIsMissing(string? name)
    {
        var result = WorkflowDefinition.Create(
            TestData.TenantId, name, WorkflowTriggerType.SystemEvent, "leave.requested", TestData.NewSteps(), TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.DefinitionNameRequired);
    }

    [Theory]
    [InlineData(WorkflowTriggerType.SystemEvent)]
    [InlineData(WorkflowTriggerType.Scheduled)]
    public void Create_Fails_WhenTriggerExpressionIsMissing_ForEventOrScheduledTriggers(WorkflowTriggerType triggerType)
    {
        var result = WorkflowDefinition.Create(
            TestData.TenantId, "Leave Approval", triggerType, null, TestData.NewSteps(), TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.TriggerExpressionRequired);
    }

    [Theory]
    [InlineData(WorkflowTriggerType.Manual)]
    [InlineData(WorkflowTriggerType.Api)]
    public void Create_Succeeds_WithoutTriggerExpression_ForManualOrApiTriggers(WorkflowTriggerType triggerType)
    {
        var result = WorkflowDefinition.Create(
            TestData.TenantId, "Offboarding", triggerType, null, TestData.NewSteps(), TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_Fails_WhenStepsAreEmpty()
    {
        var result = WorkflowDefinition.Create(
            TestData.TenantId, "Leave Approval", WorkflowTriggerType.SystemEvent, "leave.requested", [], TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.StepsRequired);
    }

    [Fact]
    public void CreateNewDraftVersion_Succeeds_WhenNoDraftExists()
    {
        var definition = TestData.PublishedDefinition();

        var result = definition.CreateNewDraftVersion(TestData.NewSteps(), TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.VersionNumber.Should().Be(2);
        definition.Versions.Should().HaveCount(2);
    }

    [Fact]
    public void CreateNewDraftVersion_Fails_WhenADraftAlreadyExists()
    {
        var definition = TestData.NewDefinition();

        var result = definition.CreateNewDraftVersion(TestData.NewSteps(), TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.DraftAlreadyExists);
    }

    [Fact]
    public void PublishVersion_Succeeds_AndDeprecatesThePreviouslyPublishedVersion()
    {
        var definition = TestData.PublishedDefinition();
        definition.CreateNewDraftVersion(TestData.NewSteps(), TestData.NowUtc);

        var result = definition.PublishVersion(2, TestData.NowUtc, WorkflowCanonicalParticipantRoles.Names);

        result.IsSuccess.Should().BeTrue();
        definition.Versions.Single(v => v.VersionNumber == 2).Status.Should().Be(WorkflowDefinitionVersionStatus.Published);
        definition.Versions.Single(v => v.VersionNumber == 1).Status.Should().Be(WorkflowDefinitionVersionStatus.Deprecated);
    }

    [Fact]
    public void PublishVersion_Fails_WhenVersionNumberDoesNotExist()
    {
        var definition = TestData.NewDefinition();

        var result = definition.PublishVersion(99, TestData.NowUtc, WorkflowCanonicalParticipantRoles.Names);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.VersionNotFound);
    }

    [Fact]
    public void PublishVersion_Fails_WhenAnApprovalStepRoutesToTheRequester()
    {
        var steps = new List<WorkflowStepDefinition>
        {
            new("Self Approval", WorkflowStepType.Approval, 1, WorkflowParticipantType.DynamicRequester, null, null, null),
        };
        var definition = TestData.NewDefinition(steps: steps);

        var result = definition.PublishVersion(1, TestData.NowUtc, WorkflowCanonicalParticipantRoles.Names);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.SelfApprovalRoutingNotAllowed);
    }

    [Fact]
    public void PublishVersion_Fails_WhenAnApprovalStepUsesANonCanonicalRole()
    {
        var steps = new List<WorkflowStepDefinition>
        {
            new("Some Approval", WorkflowStepType.Approval, 1, WorkflowParticipantType.Role, "NotACanonicalRole", null, null),
        };
        var definition = TestData.NewDefinition(steps: steps);

        var result = definition.PublishVersion(1, TestData.NowUtc, WorkflowCanonicalParticipantRoles.Names);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.InvalidParticipantRoleName);
    }

    [Fact]
    public void PublishVersion_Throws_WhenCanonicalRoleSetIsNull()
    {
        var definition = TestData.NewDefinition();

        var act = () => definition.PublishVersion(1, TestData.NowUtc, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void PublishVersion_Fails_WhenVersionIsNotDraft()
    {
        var definition = TestData.PublishedDefinition();

        var result = definition.PublishVersion(1, TestData.NowUtc, WorkflowCanonicalParticipantRoles.Names);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.InvalidVersionLifecycleTransition);
    }

    [Fact]
    public void DeprecateVersion_Succeeds_FromPublished()
    {
        var definition = TestData.PublishedDefinition();

        var result = definition.DeprecateVersion(1);

        result.IsSuccess.Should().BeTrue();
        definition.Versions[0].Status.Should().Be(WorkflowDefinitionVersionStatus.Deprecated);
    }

    [Fact]
    public void DeprecateVersion_Fails_WhenVersionIsNotPublished()
    {
        var definition = TestData.NewDefinition();

        var result = definition.DeprecateVersion(1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.InvalidVersionLifecycleTransition);
    }

    [Fact]
    public void DeprecateVersion_Fails_WhenVersionNumberDoesNotExist()
    {
        var definition = TestData.NewDefinition();

        var result = definition.DeprecateVersion(99);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(WorkflowEngineErrors.VersionNotFound);
    }

    [Fact]
    public void GetPublishedVersion_ReturnsNull_WhenNoVersionIsPublished()
    {
        var definition = TestData.NewDefinition();

        definition.GetPublishedVersion().Should().BeNull();
    }

    [Fact]
    public void GetPublishedVersion_ReturnsThePublishedVersion()
    {
        var definition = TestData.PublishedDefinition();

        definition.GetPublishedVersion().Should().NotBeNull();
        definition.GetPublishedVersion()!.VersionNumber.Should().Be(1);
    }
}
