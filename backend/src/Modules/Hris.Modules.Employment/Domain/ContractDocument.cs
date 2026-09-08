using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// Represents a document associated with an Employment Contract -- a reference to
/// Document Management-stored content, never the content itself
/// (employment-documents.md: "store documents through the Document Management
/// framework; never implement storage in the module"). Source:
/// docs/04-modules/employment/domain/entities.md, ContractDocument. A child Entity
/// of <see cref="EmploymentContract"/>, never an Aggregate Root of its own; its
/// constructor is <c>internal</c>. Full upload/retrieval integration is out of
/// scope for this Sprint, matching Position's own deferred Position Documents.
/// </summary>
public sealed class ContractDocument : Entity<ContractDocumentId>
{
    public string DocumentType { get; }

    public string StorageReference { get; }

    public int Version { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    internal ContractDocument(
        ContractDocumentId id, string documentType, string storageReference, int version, DateTimeOffset createdAtUtc)
        : base(id)
    {
        DocumentType = documentType;
        StorageReference = storageReference;
        Version = version;
        CreatedAtUtc = createdAtUtc;
    }
}
