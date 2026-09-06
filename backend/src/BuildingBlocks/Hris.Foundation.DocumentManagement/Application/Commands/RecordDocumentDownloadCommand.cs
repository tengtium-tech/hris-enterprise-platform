using Hris.Application.Abstractions;
using Hris.Foundation.DocumentManagement.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.DocumentManagement.Application.Commands;

/// <summary>
/// Records that a document's own current version was downloaded, per this framework's
/// own Security Considerations ("Every document access should be auditable").
/// </summary>
public sealed record RecordDocumentDownloadCommand(Guid DocumentId, Guid TenantId, Guid DownloadedByUserId) : ICommand<Result>;

internal sealed class RecordDocumentDownloadCommandHandler : IRequestHandler<RecordDocumentDownloadCommand, Result>
{
    private readonly IDocumentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RecordDocumentDownloadCommandHandler(IDocumentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RecordDocumentDownloadCommand request, CancellationToken cancellationToken)
    {
        var documentResult = await DocumentLookup.LoadForTenantAsync(_repository, request.DocumentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return documentResult.IsFailure
            ? Result.Failure(documentResult.Error)
            : documentResult.Value.RecordDownload(request.DownloadedByUserId, _timeProvider.GetUtcNow());
    }
}
