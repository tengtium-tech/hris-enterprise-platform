using Hris.Application.Abstractions;
using Hris.Foundation.DocumentManagement.Application.Dtos;
using Hris.Foundation.DocumentManagement.Application.Mapping;
using Hris.Foundation.DocumentManagement.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.DocumentManagement.Application.Queries;

/// <summary>
/// Lists every attachment for one business entity, per this framework's own Search
/// section ("The framework should support searching by... Business Entity").
/// </summary>
public sealed record ListDocumentAttachmentsQuery(Guid TenantId, string EntityType, Guid EntityId)
    : IQuery<Result<IReadOnlyList<DocumentAttachmentDto>>>;

internal sealed class ListDocumentAttachmentsQueryHandler
    : IRequestHandler<ListDocumentAttachmentsQuery, Result<IReadOnlyList<DocumentAttachmentDto>>>
{
    private readonly IDocumentAttachmentRepository _repository;

    public ListDocumentAttachmentsQueryHandler(IDocumentAttachmentRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<DocumentAttachmentDto>>> Handle(
        ListDocumentAttachmentsQuery request, CancellationToken cancellationToken)
    {
        var attachments = await _repository
            .ListByEntityAsync(request.TenantId, request.EntityType, request.EntityId, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<DocumentAttachmentDto> dtos = attachments.Select(DocumentManagementMapper.ToDto).ToList();
        return Result.Success(dtos);
    }
}
