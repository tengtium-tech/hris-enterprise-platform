namespace Hris.Foundation.WorkflowEngine.Domain;

/// <summary>
/// One step's own participation in a <see cref="WorkflowDefinitionVersion"/>, per
/// workflow-engine.md's own Workflow Composition ("Trigger -&gt; Conditions -&gt; Approval
/// Steps -&gt; Actions -&gt; Notifications -&gt; Completed") and Workflow Designer Node Types
/// table. A plain positional record, not a <see cref="Hris.SharedKernel.ValueObject"/>-derived
/// type with its own <c>Create</c> factory -- the identical "plain record, persisted as
/// a single JSON column, no independent per-row query need of its own component parts"
/// choice <c>SearchFieldDefinition</c>'s own remarks make, and for the same reason: this
/// framework never queries "all steps of type Approval across every definition," only
/// ever reads a whole ordered list belonging to one already-loaded
/// <see cref="WorkflowDefinitionVersion"/>.
///
/// <see cref="ActionName"/> is a generic, opaque string reference to a module's own
/// public command -- never a strongly-typed reference to any one module's command type,
/// the identical "generic by design... this framework serves every business module"
/// choice <c>IssuedNumber.AssignedToType</c>/<c>AssignedToReferenceId</c> already make
/// for the same cross-module-reference problem, and matching workflow-engine.md's own
/// Actions section naming HR/Payroll/Attendance/Notification/Integration/Control
/// actions across modules this framework does not itself own or reference by project.
///
/// Only <see cref="WorkflowDefinition.PublishVersion"/> validates
/// <see cref="ParticipantType"/>/<see cref="ParticipantRoleName"/> shape (self-approval
/// routing, canonical role names) -- deeper condition/branch/loop evaluation against
/// live business data is this framework's own deliberately excluded scope this Sprint
/// (see <c>DependencyInjection.cs</c>'s own remarks), the identical "records the
/// configuration, does not build the runtime that walks it" split
/// <c>StatutoryTableVersion.ScheduleData</c>'s own remarks already draw for a
/// differently-shaped but equally out-of-scope interpretation problem.
/// </summary>
public sealed record WorkflowStepDefinition(
    string StepName,
    WorkflowStepType StepType,
    int Order,
    WorkflowParticipantType? ParticipantType,
    string? ParticipantRoleName,
    string? ActionName,
    string? NotificationTemplateKey);
