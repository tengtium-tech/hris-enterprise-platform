using Hris.Foundation.WorkflowEngine.Domain;

namespace Hris.Foundation.WorkflowEngine.Tests;

/// <summary>
/// Valid-default builders per docs/09-testing/unit-and-integration-testing.md 2.4:
/// "Construct aggregates through builders that supply valid defaults, so each test
/// specifies only the values relevant to what it verifies." A fixed clock
/// (<see cref="NowUtc"/>), never <c>DateTimeOffset.UtcNow</c>, per that same document's
/// own 2.1 ("must not touch... a clock").
/// </summary>
internal static class TestData
{
    public static readonly DateTimeOffset NowUtc = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    public static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static readonly Guid InitiatorUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static List<WorkflowStepDefinition> NewSteps() =>
    [
        new WorkflowStepDefinition("Manager Approval", WorkflowStepType.Approval, 1, WorkflowParticipantType.Role, "PeopleManager", null, null),
        new WorkflowStepDefinition("Deduct Leave Credits", WorkflowStepType.Action, 2, null, null, "Leave.DeductCredits", null),
        new WorkflowStepDefinition("Notify Employee", WorkflowStepType.Notification, 3, null, null, null, "LeaveApproved"),
    ];

    public static WorkflowDefinition NewDefinition(
        Guid? tenantId = null,
        string name = "Leave Approval",
        WorkflowTriggerType triggerType = WorkflowTriggerType.SystemEvent,
        string? triggerExpression = "leave.requested",
        List<WorkflowStepDefinition>? steps = null,
        DateTimeOffset? nowUtc = null) =>
        WorkflowDefinition.Create(tenantId ?? TenantId, name, triggerType, triggerExpression, steps ?? NewSteps(), nowUtc ?? NowUtc).Value;

    public static WorkflowDefinition PublishedDefinition(Guid? tenantId = null, DateTimeOffset? nowUtc = null)
    {
        var definition = NewDefinition(tenantId, nowUtc: nowUtc);
        definition.PublishVersion(1, nowUtc ?? NowUtc, WorkflowCanonicalParticipantRoles.Names);
        return definition;
    }

    public static WorkflowInstance SubmittedInstance(
        Guid? tenantId = null,
        WorkflowDefinitionId? workflowDefinitionId = null,
        int workflowDefinitionVersionNumber = 1,
        Guid? initiatedByUserId = null,
        DateTimeOffset? nowUtc = null) =>
        WorkflowInstance.Trigger(
            tenantId ?? TenantId,
            workflowDefinitionId ?? new WorkflowDefinitionId(Guid.NewGuid()),
            workflowDefinitionVersionNumber,
            "leave-request-0001",
            initiatedByUserId ?? InitiatorUserId,
            nowUtc ?? NowUtc).Value;

    public static WorkflowInstance InProgressInstance(Guid? tenantId = null, DateTimeOffset? nowUtc = null)
    {
        var instance = SubmittedInstance(tenantId, nowUtc: nowUtc);
        instance.Advance(1, nowUtc ?? NowUtc);
        return instance;
    }

    public static WorkflowInstance PendingApprovalInstance(Guid? tenantId = null, DateTimeOffset? nowUtc = null)
    {
        var instance = InProgressInstance(tenantId, nowUtc);
        instance.RequestApproval();
        return instance;
    }

    public static WorkflowTask PendingTask(
        Guid? tenantId = null,
        WorkflowInstanceId? workflowInstanceId = null,
        Guid? assignedToUserId = null,
        int escalationLevel = 0,
        DateTimeOffset? nowUtc = null) =>
        WorkflowTask.Create(
            tenantId ?? TenantId,
            workflowInstanceId ?? new WorkflowInstanceId(Guid.NewGuid()),
            "Manager Approval",
            1,
            WorkflowParticipantType.NamedUser,
            null,
            assignedToUserId ?? Guid.Parse("33333333-3333-3333-3333-333333333333"),
            escalationLevel,
            nowUtc ?? NowUtc).Value;
}
