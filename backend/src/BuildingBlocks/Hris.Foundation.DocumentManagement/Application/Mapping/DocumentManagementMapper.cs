using Hris.Foundation.DocumentManagement.Application.Dtos;
using Hris.Foundation.DocumentManagement.Domain;
using Hris.SharedKernel;

namespace Hris.Foundation.DocumentManagement.Application.Mapping;

/// <summary>
/// Domain-to-DTO mapping, kept as a plain static class rather than a library such as
/// AutoMapper, per mapping.md's own stated preference for explicit mapping code -- the
/// identical choice every other Sprint 3/4/5 framework's own mapper already
/// establishes.
/// </summary>
internal static class DocumentManagementMapper
{
    /// <summary>
    /// Parses the wire-format classification string every command carries across the
    /// MediatR boundary as a raw primitive, the same "carries raw primitives, not
    /// Domain Value Objects" choice <c>RequestFileUploadCommand</c>'s own remarks state
    /// for itself. Case-insensitive and rejects any value outside
    /// <see cref="DocumentClassification"/>'s own defined members, including an
    /// out-of-range numeric string <see cref="Enum.TryParse{TEnum}(string,bool,out TEnum)"/>
    /// alone would otherwise accept.
    /// </summary>
    public static Result<DocumentClassification> ParseClassification(string? value)
    {
        return Enum.TryParse<DocumentClassification>(value, ignoreCase: true, out var classification)
            && Enum.IsDefined(classification)
            ? Result.Success(classification)
            : Result.Failure<DocumentClassification>(DocumentErrors.InvalidClassification);
    }

    public static DocumentDto ToDto(Document document) => new(
        document.Id.Value,
        document.TenantId,
        document.DocumentNumber,
        document.Title,
        document.Description,
        document.Category,
        document.Classification.ToString(),
        document.OwnerUserId,
        document.CreatedAtUtc,
        document.CompanyId,
        document.DepartmentId,
        document.EffectiveDate,
        document.ExpirationDate,
        document.Tags,
        document.Status.ToString(),
        document.CurrentVersion is null ? null : ToDto(document.CurrentVersion),
        document.Versions.Count);

    public static DocumentVersionDto ToDto(DocumentVersion version) => new(
        version.Id.Value,
        version.MajorVersion,
        version.MinorVersion,
        version.StoredFileId,
        version.CreatedByUserId,
        version.CreatedAtUtc,
        version.ChangeSummary,
        version.Status.ToString());

    public static DocumentSummaryDto ToSummaryDto(Document document) => new(
        document.Id.Value,
        document.Title,
        document.Category,
        document.Classification.ToString(),
        document.Status.ToString(),
        document.Versions.Count);

    public static DocumentAttachmentDto ToDto(DocumentAttachment attachment) => new(
        attachment.Id.Value,
        attachment.DocumentId.Value,
        attachment.EntityType,
        attachment.EntityId,
        attachment.AttachedByUserId,
        attachment.AttachedAtUtc,
        attachment.Status.ToString(),
        attachment.DetachedAtUtc);
}
