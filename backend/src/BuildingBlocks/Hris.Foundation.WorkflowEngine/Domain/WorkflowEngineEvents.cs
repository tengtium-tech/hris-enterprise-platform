using Hris.SharedKernel;

namespace Hris.Foundation.WorkflowEngine.Domain;

/// <summary>
/// workflow-engine.md's own Domain Events section names exactly nine events -- every
/// one implemented here, split five/four across <see cref="WorkflowInstance"/> and
/// <see cref="WorkflowTask"/>. <see cref="WorkflowStarted"/> fires when
/// <see cref="WorkflowInstance.Trigger"/> creates the instance (entering
/// <see cref="WorkflowInstanceStatus.Submitted"/> directly, per that status's own
/// remarks); <see cref="WorkflowSubmitted"/> fires on the first
/// <see cref="WorkflowInstance.Advance"/> call, the Submitted -&gt;
/// <see cref="WorkflowInstanceStatus.InProgress"/> transition -- the document names
/// both events but its own Lifecycle diagram has no distinct "Started" state to hang
/// the first one on, so this is where the two are told apart here. Task-level
/// <see cref="WorkflowTask.Reject"/> raises no event of its own:
/// <see cref="WorkflowRejected"/> is raised once, by
/// <see cref="WorkflowInstance.Reject"/>, avoiding the same outcome being recorded
/// twice for one instance.
/// </summary>
public sealed record WorkflowStarted(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    WorkflowInstanceId WorkflowInstanceId,
    Guid TenantId,
    WorkflowDefinitionId WorkflowDefinitionId,
    int WorkflowDefinitionVersionNumber) : IDomainEvent;

public sealed record WorkflowSubmitted(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    WorkflowInstanceId WorkflowInstanceId) : IDomainEvent;

public sealed record WorkflowAssigned(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    WorkflowTaskId WorkflowTaskId,
    WorkflowInstanceId WorkflowInstanceId,
    Guid TenantId) : IDomainEvent;

public sealed record WorkflowApproved(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    WorkflowTaskId WorkflowTaskId,
    WorkflowInstanceId WorkflowInstanceId) : IDomainEvent;

public sealed record WorkflowRejected(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    WorkflowInstanceId WorkflowInstanceId,
    string Reason) : IDomainEvent;

public sealed record WorkflowEscalated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    WorkflowTaskId WorkflowTaskId,
    WorkflowInstanceId WorkflowInstanceId,
    int EscalationLevel) : IDomainEvent;

public sealed record WorkflowDelegated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    WorkflowTaskId WorkflowTaskId,
    Guid DelegatedToUserId) : IDomainEvent;

public sealed record WorkflowCompleted(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    WorkflowInstanceId WorkflowInstanceId) : IDomainEvent;

public sealed record WorkflowCancelled(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    WorkflowInstanceId WorkflowInstanceId,
    string Reason) : IDomainEvent;
