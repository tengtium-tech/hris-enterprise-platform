namespace Hris.Foundation.DocumentManagement.Application.Dtos;

/// <summary>
/// The read-side shape <c>GetDocumentQuery</c> returns, per dto-design.md's own
/// convention.
/// </summary>
public sealed record DocumentDto(
    Guid DocumentId,
    Guid TenantId,
    string? DocumentNumber,
    string Title,
    string? Description,
    string Category,
    string Classification,
    Guid OwnerUserId,
    DateTimeOffset CreatedAtUtc,
    Guid? CompanyId,
    Guid? DepartmentId,
    DateOnly? EffectiveDate,
    DateOnly? ExpirationDate,
    IReadOnlyList<string> Tags,
    string Status,
    DocumentVersionDto? CurrentVersion,
    int VersionCount);

public sealed record DocumentVersionDto(
    Guid DocumentVersionId,
    int MajorVersion,
    int MinorVersion,
    Guid StoredFileId,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAtUtc,
    string? ChangeSummary,
    string Status);

/// <summary>
/// The lighter-weight shape <c>SearchDocumentsQuery</c> returns per matching row --
/// api-standards.md's own Response Shapes guidance that a list endpoint returns a
/// summary projection, not every field the single-resource endpoint does.
/// </summary>
public sealed record DocumentSummaryDto(
    Guid DocumentId,
    string Title,
    string Category,
    string Classification,
    string Status,
    int VersionCount);
