namespace Hris.Foundation.WorkflowEngine.Domain;

/// <summary>
/// workflow-engine.md's own Workflow Participant section, verbatim: role-based
/// participants are represented by <see cref="Role"/> (see
/// <see cref="WorkflowStepDefinition.ParticipantRoleName"/> for which canonical role
/// names are actually allowed); the four non-role kinds that section names --
/// "Named user," "Dynamic resolution... the requester's reporting manager," "External
/// approver," and "System" -- map to <see cref="NamedUser"/>, <see cref="DynamicManager"/>,
/// <see cref="ExternalApprover"/>, and <see cref="System"/> respectively.
///
/// <see cref="DynamicRequester"/> is NOT one of that section's own four non-role kinds
/// -- it exists here specifically so <see cref="WorkflowDefinition.PublishVersion"/> has
/// a structural value to reject: an Approval step whose participant resolves to "the
/// requester themselves" is self-approval by construction, for every instance this
/// definition version will ever produce, which is exactly what the Permissions
/// section's own "Self-approval cannot occur... fails validation at publication" rule
/// requires catching before publish, not per-instance at approval time.
/// </summary>
public enum WorkflowParticipantType
{
    Role = 0,
    NamedUser = 1,
    DynamicManager = 2,
    DynamicRequester = 3,
    ExternalApprover = 4,
    System = 5,
}
