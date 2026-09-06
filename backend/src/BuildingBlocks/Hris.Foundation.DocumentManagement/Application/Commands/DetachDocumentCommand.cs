using Hris.Application.Abstractions;
using Hris.Foundation.DocumentManagement.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.DocumentManagement.Application.Commands;

/// <summary>
/// Soft-detaches a document from a business entity -- see
/// <see cref="DocumentAttachment.Detach"/>'s own remarks for why this preserves the
/// row rather than deleting it.
/// </summary>
public sealed record DetachDocumentCommand(Guid DocumentAttachmentId, Guid TenantId) : ICommand<Result>;

internal sealed class DetachDocumentCommandHandler : IRequestHandler<DetachDocumentCommand, Result>
{
    private readonly IDocumentAttachmentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public DetachDocumentCommandHandler(IDocumentAttachmentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(DetachDocumentCommand request, CancellationToken cancellationToken)
    {
        var attachment = await _repository.GetByIdAsync(new DocumentAttachmentId(request.DocumentAttachmentId), cancellationToken)
            .ConfigureAwait(false);

        if (attachment is null || attachment.TenantId != request.TenantId)
        {
            return Result.Failure(DocumentErrors.DocumentAttachmentNotFound);
        }

        return attachment.Detach(_timeProvider.GetUtcNow());
    }
}
