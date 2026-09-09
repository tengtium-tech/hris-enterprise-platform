namespace Hris.Modules.Workflow.Domain;

/// <summary>
/// Source: docs/04-modules/workflow/domain/workflow-definitions.md's own lifecycle
/// table. Deprecated is terminal: a withdrawn definition is authored anew rather
/// than reinstated, because the organizational context that made the old one
/// correct may no longer hold and silent reinstatement would skip the review
/// authoring is supposed to represent.
/// </summary>
public enum DefinitionStatus
{
    /// <summary>Freely editable, not executable. No instance can start against it.</summary>
    Draft = 0,

    /// <summary>Executable and immutable (WR-004). A change is a new version, never an in-place edit.</summary>
    Published = 1,

    /// <summary>Withdrawn from new instances. Running instances are unaffected.</summary>
    Deprecated = 2,
}
