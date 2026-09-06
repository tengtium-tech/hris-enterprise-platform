using Hris.SharedKernel;

namespace Hris.Foundation.DocumentManagement.Domain;

/// <summary>
/// One business-level version of a <see cref="Document"/>. Source:
/// docs/03-foundation/document-management.md, Version ("Each version should maintain:
/// Version Number, Created Date, Author, Change Summary, Status") and Version
/// Management ("Major Versions, Minor Versions... Previous versions should never be
/// overwritten"). A child Entity, never an Aggregate Root of its own
/// (aggregate-design-rules.md Rule 7) -- its constructor and mutating method are
/// <c>internal</c>, reachable only through <see cref="Document"/>, the same shape
/// <c>FileVersion</c> already establishes for the identical child-of-versioned-
/// aggregate shape in File Storage Framework.
///
/// <see cref="StoredFileId"/> is deliberately a plain <see cref="Guid"/>, not a
/// strongly typed reference into File Storage Framework's own project -- this
/// framework's own Scope excludes "Physical File Storage" ("Physical storage is
/// provided by the File Storage Framework"), and per this codebase's own established
/// discipline (Notification Framework's own <c>DependencyInjection.cs</c> remarks: "no
/// Sprint 4/5 framework in this solution takes a ProjectReference on another Sprint
/// 3/4/5 framework's own project, and this one is no exception"), a lateral Foundation
/// framework is referenced by a caller-supplied identifier only, never a compile-time
/// dependency. A caller obtains a real, already-uploaded <c>StoredFileId</c> from File
/// Storage Framework's own <c>RequestFileUploadCommand</c>/<c>ConfirmFileStoredCommand</c>
/// flow before ever calling <see cref="Document.AddVersion"/> with it; this framework
/// never validates that the given identifier actually resolves to an
/// <c>Available</c> stored file, the same trust boundary every other cross-framework
/// caller-supplied identifier in this codebase already accepts.
/// </summary>
public sealed class DocumentVersion : Entity<DocumentVersionId>
{
    public int MajorVersion { get; }

    public int MinorVersion { get; }

    public Guid StoredFileId { get; }

    public Guid CreatedByUserId { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public string? ChangeSummary { get; }

    public DocumentVersionStatus Status { get; private set; }

    internal DocumentVersion(
        DocumentVersionId id,
        int majorVersion,
        int minorVersion,
        Guid storedFileId,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc,
        string? changeSummary)
        : base(id)
    {
        MajorVersion = majorVersion;
        MinorVersion = minorVersion;
        StoredFileId = storedFileId;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
        ChangeSummary = changeSummary;
        Status = DocumentVersionStatus.Active;
    }

    /// <summary>
    /// Called only on the prior <see cref="Document.CurrentVersion"/> the instant a new
    /// version is recorded -- never overwritten, never removed from
    /// <see cref="Document.Versions"/>, per this framework's own "Previous versions
    /// should never be overwritten" requirement.
    /// </summary>
    internal void Supersede()
    {
        Status = DocumentVersionStatus.Superseded;
    }
}
