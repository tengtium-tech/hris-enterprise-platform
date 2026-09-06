using Hris.SharedKernel;

namespace Hris.Foundation.DocumentManagement.Domain;

/// <summary>
/// This bounded context's own reusable error catalog, per error-pattern.md's "Error
/// Catalog" section. <see cref="DocumentNotFound"/> is deliberately the one error
/// returned both for a genuinely missing document and for a document that exists but
/// belongs to another tenant (<c>CTR-ISO-002</c>: "must return not-found, not... a
/// permission error that confirms the record's existence") -- there is no separate
/// "wrong tenant" error for exactly this reason.
/// </summary>
public static class DocumentErrors
{
    public static readonly Error TitleRequired = new(
        "DocumentManagement.TitleRequired",
        "A document title is required.",
        ErrorCategory.Validation);

    public static readonly Error TitleTooLong = new(
        "DocumentManagement.TitleTooLong",
        "A document title cannot exceed 260 characters.",
        ErrorCategory.Validation);

    public static readonly Error CategoryRequired = new(
        "DocumentManagement.CategoryRequired",
        "A document category is required.",
        ErrorCategory.Validation);

    public static readonly Error InvalidClassification = new(
        "DocumentManagement.InvalidClassification",
        "The given classification is not a recognized value.",
        ErrorCategory.Validation);

    public static readonly Error DocumentNotFound = new(
        "DocumentManagement.DocumentNotFound",
        "No document exists for the given identifier.",
        ErrorCategory.NotFound);

    public static readonly Error InvalidDocumentLifecycleTransition = new(
        "DocumentManagement.InvalidDocumentLifecycleTransition",
        "This transition is not valid from the document's current status.",
        ErrorCategory.Domain);

    public static readonly Error NoCurrentVersion = new(
        "DocumentManagement.NoCurrentVersion",
        "This document has no current version to act on.",
        ErrorCategory.Domain);

    public static readonly Error StoredFileIdRequired = new(
        "DocumentManagement.StoredFileIdRequired",
        "A stored file identifier is required to record a document version.",
        ErrorCategory.Validation);

    public static readonly Error DocumentAttachmentNotFound = new(
        "DocumentManagement.DocumentAttachmentNotFound",
        "No document attachment exists for the given identifier.",
        ErrorCategory.NotFound);

    public static readonly Error EntityTypeRequired = new(
        "DocumentManagement.EntityTypeRequired",
        "A business entity type is required to attach a document.",
        ErrorCategory.Validation);

    public static readonly Error InvalidDocumentAttachmentTransition = new(
        "DocumentManagement.InvalidDocumentAttachmentTransition",
        "This transition is not valid from the attachment's current status.",
        ErrorCategory.Domain);
}
