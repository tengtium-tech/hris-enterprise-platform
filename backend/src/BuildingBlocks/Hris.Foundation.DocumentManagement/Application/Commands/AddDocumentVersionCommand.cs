using Hris.Application.Abstractions;
using Hris.Foundation.DocumentManagement.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.DocumentManagement.Application.Commands;

/// <summary>
/// Records a new business-level version against an already-uploaded physical file,
/// per document-management.md's own "Document Upload"/"Document Versioning" Scope
/// items. <see cref="StoredFileId"/> is a plain <see cref="Guid"/> the caller already
/// obtained from File Storage Framework's own upload flow -- see
/// <c>DocumentVersion</c>'s own remarks for why this framework never takes a
/// compile-time dependency on that one.
/// </summary>
public sealed record AddDocumentVersionCommand(
    Guid DocumentId,
    Guid TenantId,
    Guid StoredFileId,
    bool IsMajorVersion,
    string? ChangeSummary,
    Guid CreatedByUserId) : ICommand<Result<Guid>>;

internal sealed class AddDocumentVersionCommandHandler : IRequestHandler<AddDocumentVersionCommand, Result<Guid>>
{
    private readonly IDocumentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public AddDocumentVersionCommandHandler(IDocumentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(AddDocumentVersionCommand request, CancellationToken cancellationToken)
    {
        var documentResult = await DocumentLookup.LoadForTenantAsync(_repository, request.DocumentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (documentResult.IsFailure)
        {
            return Result.Failure<Guid>(documentResult.Error);
        }

        return documentResult.Value.AddVersion(
            request.StoredFileId, request.IsMajorVersion, request.ChangeSummary, request.CreatedByUserId, _timeProvider.GetUtcNow());
    }
}
