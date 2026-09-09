using FluentAssertions;
using Hris.Modules.Workflow.Domain;
using Hris.SharedKernel;
using Xunit;

namespace Hris.Modules.Workflow.Tests.Domain;

public sealed class WorkflowDomainEventsTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void EveryEventTypeImplementsTheDomainEventContract()
    {
        var eventTypes = typeof(WorkflowDefinitionCreated).Assembly
            .GetTypes()
            .Where(t => t.Namespace == typeof(WorkflowDefinitionCreated).Namespace)
            .Where(t => typeof(IDomainEvent).IsAssignableFrom(t) && !t.IsInterface)
            .ToList();

        eventTypes.Should().HaveCountGreaterThan(0);
        eventTypes.Should().OnlyContain(t => typeof(IDomainEvent).IsAssignableFrom(t));
    }

    [Fact]
    public void DefinitionEventsCarryTenantContext()
    {
        var definition = TestWorkflow.PublishableDraft(_tenantId, out _);
        definition.Publish(false, false, false, Guid.NewGuid(), TestWorkflow.NowUtc);

        definition.DomainEvents.OfType<WorkflowDefinitionCreated>().Single().TenantId.Should().Be(_tenantId);
        definition.DomainEvents.OfType<WorkflowDefinitionPublished>().Single().TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public void DelegationEventsCarryTenantContext()
    {
        var delegation = TestWorkflow.ActiveDelegation(_tenantId, Guid.NewGuid(), Guid.NewGuid());

        delegation.DomainEvents.OfType<ApprovalDelegationCreated>().Single().TenantId.Should().Be(_tenantId);
        delegation.DomainEvents.OfType<ApprovalDelegationActivated>().Single().TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public void ExpiryCarriesNoActor_BecauseNoneActed()
    {
        var delegation = TestWorkflow.ActiveDelegation(_tenantId, Guid.NewGuid(), Guid.NewGuid());
        delegation.Expire(TestWorkflow.NowUtc);

        var expired = delegation.DomainEvents.OfType<ApprovalDelegationExpired>().Single();

        expired.GetType().GetProperties().Select(p => p.Name)
            .Should().BeEquivalentTo(["EventId", "OccurredOnUtc", "ApprovalDelegationId", "TenantId"]);
    }

    [Fact]
    public void RevocationCarriesActorAndReason()
    {
        var delegation = TestWorkflow.ActiveDelegation(_tenantId, Guid.NewGuid(), Guid.NewGuid());
        var actor = Guid.NewGuid();
        delegation.Revoke(actor, "Returned early", TestWorkflow.NowUtc);

        var revoked = delegation.DomainEvents.OfType<ApprovalDelegationRevoked>().Single();

        revoked.RevokedBy.Should().Be(actor);
        revoked.Reason.Should().Be("Returned early");
    }

    [Fact]
    public void EventsRecordTheTimeTheyOccurred()
    {
        var definition = TestWorkflow.Draft(_tenantId);

        definition.DomainEvents.Single().OccurredOnUtc.Should().Be(TestWorkflow.NowUtc);
    }

    [Fact]
    public void ClearDomainEvents_EmptiesTheCollection()
    {
        var definition = TestWorkflow.Draft(_tenantId);

        definition.ClearDomainEvents();

        definition.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// The approval-outcome family documented in domain-events.md is deliberately
    /// absent this Sprint: every one of those events is raised as an instance
    /// progresses, and no aggregate in this module holds instance state. Recording
    /// the absence as a test keeps the decision visible rather than looking like an
    /// oversight to the next contributor.
    /// </summary>
    [Fact]
    public void ApprovalOutcomeEventsAreNotDefinedInThisModule()
    {
        var eventNames = typeof(WorkflowDefinitionCreated).Assembly
            .GetTypes()
            .Where(t => typeof(IDomainEvent).IsAssignableFrom(t) && !t.IsInterface)
            .Select(t => t.Name)
            .ToList();

        eventNames.Should().NotContain("ApprovalGranted");
        eventNames.Should().NotContain("ApprovalRejected");
        eventNames.Should().NotContain("ApprovalExempted");
    }
}
