using Hris.Application.Abstractions;
using Hris.Foundation.DocumentManagement.Application.Dtos;
using Hris.Foundation.DocumentManagement.Application.Mapping;
using Hris.Foundation.DocumentManagement.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.DocumentManagement.Application.Queries;

/// <summary>
/// Reads one document back by its own identifier, tenant-checked via
/// <see cref="DocumentLookup"/> per CTR-ISO-001/CTR-ISO-002.
/// </summary>
public sealed record GetDocumentQuery(Guid DocumentId, Guid TenantId) : IQuery<Result<DocumentDto>>;

internal sealed class GetDocumentQueryHandler : IRequestHandler<GetDocumentQuery, Result<DocumentDto>>
{
    private readonly IDocumentRepository _repository;

    public GetDocumentQueryHandler(IDocumentRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<DocumentDto>> Handle(GetDocumentQuery request, CancellationToken cancellationToken)
    {
        var documentResult = await DocumentLookup.LoadForTenantAsync(_repository, request.DocumentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return documentResult.IsFailure
            ? Result.Failure<DocumentDto>(documentResult.Error)
            : Result.Success(DocumentManagementMapper.ToDto(documentResult.Value));
    }
}
