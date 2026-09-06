using Hris.Application.Abstractions;
using Hris.Foundation.DocumentManagement.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.DocumentManagement.Application.Commands;

/// <summary>
/// Associates a document with a business entity, per document-management.md's own
/// "Attachment Management" Scope item. Verifies the referenced <see cref="Document"/>
/// exists and belongs to <see cref="TenantId"/> before creating the
/// <see cref="DocumentAttachment"/> -- <see cref="DocumentAttachment.Attach"/>'s own
/// remarks explain why this check belongs here, at the Application layer, rather than
/// inside that Domain factory.
/// </summary>
public sealed record AttachDocumentCommand(
    Guid DocumentId, Guid TenantId, string EntityType, Guid EntityId, Guid AttachedByUserId) : ICommand<Result<Guid>>;

internal sealed class AttachDocumentCommandHandler : IRequestHandler<AttachDocumentCommand, Result<Guid>>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentAttachmentRepository _attachmentRepository;
    private readonly TimeProvider _timeProvider;

    public AttachDocumentCommandHandler(
        IDocumentRepository documentRepository, IDocumentAttachmentRepository attachmentRepository, TimeProvider timeProvider)
    {
        _documentRepository = Guard.AgainstNull(documentRepository, nameof(documentRepository));
        _attachmentRepository = Guard.AgainstNull(attachmentRepository, nameof(attachmentRepository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(AttachDocumentCommand request, CancellationToken cancellationToken)
    {
        var documentResult = await DocumentLookup.LoadForTenantAsync(
            _documentRepository, request.DocumentId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (documentResult.IsFailure)
        {
            return Result.Failure<Guid>(documentResult.Error);
        }

        var attachmentResult = DocumentAttachment.Attach(
            documentResult.Value.Id,
            request.TenantId,
            request.EntityType,
            request.EntityId,
            request.AttachedByUserId,
            _timeProvider.GetUtcNow());

        if (attachmentResult.IsFailure)
        {
            return Result.Failure<Guid>(attachmentResult.Error);
        }

        await _attachmentRepository.AddAsync(attachmentResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(attachmentResult.Value.Id.Value);
    }
}
