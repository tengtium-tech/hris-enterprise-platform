using Hris.SharedKernel;

namespace Hris.Foundation.DocumentManagement.Domain;

/// <summary>
/// Identity of the <see cref="Document"/> Aggregate Root. Source:
/// docs/03-foundation/document-management.md, Metadata ("Document Number" is a
/// separate, business-facing identifier -- this is the aggregate's own surrogate key).
/// </summary>
public readonly record struct DocumentId(Guid Value) : IStronglyTypedId;
