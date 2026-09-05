namespace Hris.Foundation.WorkflowEngine.Domain;

/// <summary>
/// workflow-engine.md's own Workflow Versioning section: "Draft Version, Published
/// Version, Deprecated Version, Historical Version." <see cref="Deprecated"/> covers
/// both "Deprecated" and "Historical" from that list -- a version stops being the one
/// new instances start on the moment a newer sibling publishes (see
/// <see cref="WorkflowDefinition.PublishVersion"/>'s own remarks), and this framework
/// draws no further distinction between "recently superseded" and "long historical"
/// once that has happened, since neither status accepts any further transition.
/// </summary>
public enum WorkflowDefinitionVersionStatus
{
    Draft = 0,
    Published = 1,
    Deprecated = 2,
}
