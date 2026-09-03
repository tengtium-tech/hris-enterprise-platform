namespace Hris.Foundation.FileStorage.Domain;

/// <summary>
/// Source: docs/03-foundation/file-storage.md, File Lifecycle diagram: "Upload
/// Requested -> Uploaded -> Validated -> Stored -> Available -> Archived -> Deleted."
///
/// <c>Stored</c> is deliberately not its own value here. The source document draws it
/// as a box adjacent to <c>Available</c> but names no business action, condition, or
/// Domain Event that distinguishes "physically written to the provider" from "available
/// for use" -- unlike every other adjacent pair in the diagram, which the document's own
/// Domain Events section backs with a named event (<c>FileUploaded</c>,
/// <c>FileValidated</c>, and so on). Inventing a distinct trigger for a boundary the
/// source document never describes would be exactly the kind of manufactured decision
/// point CLAUDE.md's "do not manufacture decision points" warns against; collapsing the
/// two into one transition -- <see cref="StoredFile.MarkStored"/> moves status directly
/// to <see cref="Available"/> -- reads the diagram as the conceptual pipeline it is,
/// not a formal state machine specification.
/// </summary>
public enum FileLifecycleStatus
{
    UploadRequested = 0,
    Uploaded = 1,
    Validated = 2,
    Available = 3,
    Archived = 4,
    Deleted = 5,
}
