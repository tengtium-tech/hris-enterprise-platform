using Hris.Application.Abstractions;
using Hris.Foundation.DocumentManagement.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.DocumentManagement.Application.Commands;

/// <summary>
/// Updates the Metadata-section fields as one unit, per document-management.md's own
/// "Metadata Management" Scope item.
/// </summary>
public sealed record UpdateDocumentMetadataCommand(
    Guid DocumentId,
    Guid TenantId,
    string Title,
    string Category,
    string? Description,
    IReadOnlyList<string>? Tags,
    DateOnly? EffectiveDate,
    DateOnly? ExpirationDate,
    Guid? CompanyId,
    Guid? DepartmentId) : ICommand<Result>;

internal sealed class UpdateDocumentMetadataCommandHandler : IRequestHandler<UpdateDocumentMetadataCommand, Result>
{
    private readonly IDocumentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public UpdateDocumentMetadataCommandHandler(IDocumentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(UpdateDocumentMetadataCommand request, CancellationToken cancellationToken)
    {
        var documentResult = await DocumentLookup.LoadForTenantAsync(_repository, request.DocumentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (documentResult.IsFailure)
        {
            return Result.Failure(documentResult.Error);
        }

        return documentResult.Value.UpdateMetadata(
            request.Title,
            request.Description,
            request.Category,
            request.Tags,
            request.EffectiveDate,
            request.ExpirationDate,
            request.CompanyId,
            request.DepartmentId,
            _timeProvider.GetUtcNow());
    }
}
