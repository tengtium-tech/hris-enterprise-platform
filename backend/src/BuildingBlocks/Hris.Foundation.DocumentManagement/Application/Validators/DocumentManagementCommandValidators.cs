using FluentValidation;
using Hris.Foundation.DocumentManagement.Application.Commands;
using Hris.Foundation.DocumentManagement.Application.Queries;

namespace Hris.Foundation.DocumentManagement.Application.Validators;

/// <summary>
/// application-pipeline.md's Validation Behavior scope: "Required fields...
/// Business-independent validation." Deliberately does not re-check anything the
/// Domain layer's own factory/transition methods already enforce (title/category
/// shape, lifecycle-state gating) -- the identical separation every other framework's
/// own validators file states for its own set.
/// </summary>
public sealed class CreateDocumentCommandValidator : AbstractValidator<CreateDocumentCommand>
{
    public CreateDocumentCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Title).NotEmpty();
        RuleFor(c => c.Category).NotEmpty();
        RuleFor(c => c.Classification).NotEmpty();
        RuleFor(c => c.OwnerUserId).NotEmpty();
    }
}

public sealed class AddDocumentVersionCommandValidator : AbstractValidator<AddDocumentVersionCommand>
{
    public AddDocumentVersionCommandValidator()
    {
        RuleFor(c => c.DocumentId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.StoredFileId).NotEmpty();
        RuleFor(c => c.CreatedByUserId).NotEmpty();
    }
}

public sealed class UpdateDocumentMetadataCommandValidator : AbstractValidator<UpdateDocumentMetadataCommand>
{
    public UpdateDocumentMetadataCommandValidator()
    {
        RuleFor(c => c.DocumentId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Title).NotEmpty();
        RuleFor(c => c.Category).NotEmpty();
    }
}

public sealed class ReclassifyDocumentCommandValidator : AbstractValidator<ReclassifyDocumentCommand>
{
    public ReclassifyDocumentCommandValidator()
    {
        RuleFor(c => c.DocumentId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Classification).NotEmpty();
    }
}

public sealed class ReviewDocumentCommandValidator : AbstractValidator<ReviewDocumentCommand>
{
    public ReviewDocumentCommandValidator()
    {
        RuleFor(c => c.DocumentId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class ApproveDocumentCommandValidator : AbstractValidator<ApproveDocumentCommand>
{
    public ApproveDocumentCommandValidator()
    {
        RuleFor(c => c.DocumentId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class ActivateDocumentCommandValidator : AbstractValidator<ActivateDocumentCommand>
{
    public ActivateDocumentCommandValidator()
    {
        RuleFor(c => c.DocumentId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class ArchiveDocumentCommandValidator : AbstractValidator<ArchiveDocumentCommand>
{
    public ArchiveDocumentCommandValidator()
    {
        RuleFor(c => c.DocumentId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class DisposeDocumentCommandValidator : AbstractValidator<DisposeDocumentCommand>
{
    public DisposeDocumentCommandValidator()
    {
        RuleFor(c => c.DocumentId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class RecordDocumentDownloadCommandValidator : AbstractValidator<RecordDocumentDownloadCommand>
{
    public RecordDocumentDownloadCommandValidator()
    {
        RuleFor(c => c.DocumentId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.DownloadedByUserId).NotEmpty();
    }
}

public sealed class AttachDocumentCommandValidator : AbstractValidator<AttachDocumentCommand>
{
    public AttachDocumentCommandValidator()
    {
        RuleFor(c => c.DocumentId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.EntityType).NotEmpty();
        RuleFor(c => c.EntityId).NotEmpty();
        RuleFor(c => c.AttachedByUserId).NotEmpty();
    }
}

public sealed class DetachDocumentCommandValidator : AbstractValidator<DetachDocumentCommand>
{
    public DetachDocumentCommandValidator()
    {
        RuleFor(c => c.DocumentAttachmentId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
    }
}

public sealed class GetDocumentQueryValidator : AbstractValidator<GetDocumentQuery>
{
    public GetDocumentQueryValidator()
    {
        RuleFor(q => q.DocumentId).NotEmpty();
        RuleFor(q => q.TenantId).NotEmpty();
    }
}

public sealed class ListDocumentAttachmentsQueryValidator : AbstractValidator<ListDocumentAttachmentsQuery>
{
    public ListDocumentAttachmentsQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.EntityType).NotEmpty();
        RuleFor(q => q.EntityId).NotEmpty();
    }
}

public sealed class SearchDocumentsQueryValidator : AbstractValidator<SearchDocumentsQuery>
{
    public SearchDocumentsQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.Page).NotNull();
    }
}
