using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// Identity of the <see cref="ContractDocument"/> child Entity of
/// <see cref="EmploymentContract"/>. Source:
/// docs/04-modules/employment/domain/entities.md, ContractDocument Identity
/// ("ContractDocumentId"). This Entity retains only a reference to Document
/// Management-stored content, per employment-documents.md's own "store documents
/// through the Document Management framework; never implement storage in the
/// module" guidance -- full document upload/retrieval is out of scope for this
/// Sprint, matching Position's own deferred Position Documents precedent.
/// </summary>
public readonly record struct ContractDocumentId(Guid Value) : IStronglyTypedId;
