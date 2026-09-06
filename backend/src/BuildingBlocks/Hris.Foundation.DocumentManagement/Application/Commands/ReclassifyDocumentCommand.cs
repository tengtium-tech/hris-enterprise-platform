using Hris.Application.Abstractions;
using Hris.Foundation.DocumentManagement.Application.Mapping;
using Hris.Foundation.DocumentManagement.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.DocumentManagement.Application.Commands;

/// <summary>
/// Changes a document's own <see cref="DocumentClassification"/>, per
/// document-management.md's own "Document Classification" Scope item.
/// </summary>
public sealed record ReclassifyDocumentCommand(Guid DocumentId, Guid TenantId, string Classification) : ICommand<Result>;

internal sealed class ReclassifyDocumentCommandHandler : IRequestHandler<ReclassifyDocumentCommand, Result>
{
    private readonly IDocumentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ReclassifyDocumentCommandHandler(IDocumentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ReclassifyDocumentCommand request, CancellationToken cancellationToken)
    {
        var classificationResult = DocumentManagementMapper.ParseClassification(request.Classification);
        if (classificationResult.IsFailure)
        {
            return Result.Failure(classificationResult.Error);
        }

        var documentResult = await DocumentLookup.LoadForTenantAsync(_repository, request.DocumentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (documentResult.IsFailure)
        {
            return Result.Failure(documentResult.Error);
        }

        return documentResult.Value.Reclassify(classificationResult.Value, _timeProvider.GetUtcNow());
    }
}
