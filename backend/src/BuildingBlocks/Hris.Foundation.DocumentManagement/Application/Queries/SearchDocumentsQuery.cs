using Hris.Application.Abstractions;
using Hris.Application.Pagination;
using Hris.Foundation.DocumentManagement.Application.Dtos;
using Hris.Foundation.DocumentManagement.Application.Mapping;
using Hris.Foundation.DocumentManagement.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.DocumentManagement.Application.Queries;

/// <summary>
/// Backs this framework's own Search section (searching by Category, Title, Tags,
/// Classification). The first Application-layer query in this codebase to consume
/// <see cref="PageRequest"/>/<see cref="PagedResult{T}"/> from a framework's own
/// handler, rather than only from <c>Hris.Api</c> directly -- api-standards.md's own
/// Pagination section governs this query's own paging contract the identical way it
/// already governs every list endpoint.
/// </summary>
public sealed record SearchDocumentsQuery(
    Guid TenantId,
    string? Category,
    string? Classification,
    string? TitleContains,
    IReadOnlyList<string>? Tags,
    PageRequest Page) : IQuery<Result<PagedResult<DocumentSummaryDto>>>;

internal sealed class SearchDocumentsQueryHandler : IRequestHandler<SearchDocumentsQuery, Result<PagedResult<DocumentSummaryDto>>>
{
    private const int _maxPageSize = 100;

    private readonly IDocumentRepository _repository;

    public SearchDocumentsQueryHandler(IDocumentRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<PagedResult<DocumentSummaryDto>>> Handle(SearchDocumentsQuery request, CancellationToken cancellationToken)
    {
        var pageValidation = request.Page.Validate(_maxPageSize);
        if (pageValidation.IsFailure)
        {
            return Result.Failure<PagedResult<DocumentSummaryDto>>(pageValidation.Error);
        }

        DocumentClassification? classification = null;
        if (!string.IsNullOrWhiteSpace(request.Classification))
        {
            var classificationResult = DocumentManagementMapper.ParseClassification(request.Classification);
            if (classificationResult.IsFailure)
            {
                return Result.Failure<PagedResult<DocumentSummaryDto>>(classificationResult.Error);
            }

            classification = classificationResult.Value;
        }

        var skip = (request.Page.Page - 1) * request.Page.PageSize;
        var (items, totalCount) = await _repository.SearchAsync(
            request.TenantId,
            request.Category,
            classification,
            request.TitleContains,
            request.Tags,
            skip,
            request.Page.PageSize,
            cancellationToken).ConfigureAwait(false);

        IReadOnlyCollection<DocumentSummaryDto> dtos = items.Select(DocumentManagementMapper.ToSummaryDto).ToList();
        return Result.Success(new PagedResult<DocumentSummaryDto>(dtos, request.Page.Page, request.Page.PageSize, totalCount));
    }
}
