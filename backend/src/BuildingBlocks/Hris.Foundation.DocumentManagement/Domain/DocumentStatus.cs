namespace Hris.Foundation.DocumentManagement.Domain;

/// <summary>
/// Source: docs/03-foundation/document-management.md, Document Lifecycle
/// (Draft -&gt; Uploaded -&gt; Reviewed -&gt; Approved -&gt; Active -&gt; Archived -&gt; Disposed).
/// That section's own "Organizations may customize lifecycle stages" is a stated
/// future extension point, not this Sprint's own scope -- the same "records the
/// configuration, does not build the runtime that walks it" split this codebase
/// already applies elsewhere to configurable-but-not-yet-configurable behavior; this
/// Sprint implements the fixed seven-stage lifecycle exactly as documented.
/// </summary>
public enum DocumentStatus
{
    Draft = 0,
    Uploaded,
    Reviewed,
    Approved,
    Active,
    Archived,
    Disposed,
}
