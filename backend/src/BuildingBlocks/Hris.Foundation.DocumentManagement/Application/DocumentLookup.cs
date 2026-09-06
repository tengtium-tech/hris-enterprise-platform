using Hris.Foundation.DocumentManagement.Domain;
using Hris.SharedKernel;

namespace Hris.Foundation.DocumentManagement.Application;

/// <summary>
/// The one place every command/query handler that loads a <see cref="Document"/> by
/// its own identifier performs this framework's own explicit tenant-isolation check --
/// document-management.md's own AI Implementation Guidance: "Scope every document to a
/// tenant and enforce isolation on retrieval, including direct identifier access
/// (CTR-ISO-001, CTR-ISO-002)." Returning <see cref="DocumentErrors.DocumentNotFound"/>
/// for both a genuinely missing document and one that exists but belongs to a
/// different tenant is deliberate -- CTR-ISO-002: "must return not-found, not... a
/// permission error that confirms the record's existence." Factored out once, rather
/// than repeated inline in every handler, per CLAUDE.md's own "prefer structure over
/// discipline": a check this load-bearing should not depend on every future handler
/// remembering to repeat it correctly.
/// </summary>
internal static class DocumentLookup
{
    public static async Task<Result<Document>> LoadForTenantAsync(
        IDocumentRepository repository, Guid documentId, Guid tenantId, CancellationToken cancellationToken)
    {
        var document = await repository.GetByIdAsync(new DocumentId(documentId), cancellationToken).ConfigureAwait(false);

        return document is null || document.TenantId != tenantId
            ? Result.Failure<Document>(DocumentErrors.DocumentNotFound)
            : Result.Success(document);
    }
}
