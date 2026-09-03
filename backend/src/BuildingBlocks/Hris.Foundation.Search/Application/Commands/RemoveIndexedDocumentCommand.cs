using Hris.Application.Abstractions;
using Hris.Foundation.Search.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Search.Application.Commands;

/// <summary>
/// Soft-removes one indexed document -- the source module's own "this record was
/// deleted" path. Identified by <see cref="IndexedDocument"/>'s own id, not by source
/// reference: the caller that raised this command already resolved it via
/// <c>IndexDocumentCommand</c>'s own earlier response, matching every other
/// Sprint 3/4 framework's own by-id lifecycle command shape.
/// </summary>
public sealed record RemoveIndexedDocumentCommand(Guid IndexedDocumentId) : ICommand<Result>;

internal sealed class RemoveIndexedDocumentCommandHandler : IRequestHandler<RemoveIndexedDocumentCommand, Result>
{
    private readonly IIndexedDocumentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RemoveIndexedDocumentCommandHandler(IIndexedDocumentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RemoveIndexedDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await _repository
            .GetByIdAsync(new IndexedDocumentId(request.IndexedDocumentId), cancellationToken)
            .ConfigureAwait(false);

        return document is null
            ? Result.Failure(SearchErrors.IndexedDocumentNotFound)
            : document.Remove(_timeProvider.GetUtcNow());
    }
}
