using Hris.Application.Abstractions;
using Hris.Foundation.DocumentManagement.Application.Mapping;
using Hris.Foundation.DocumentManagement.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.DocumentManagement.Application.Commands;

/// <summary>
/// Registers a new document, per document-management.md's own "Document
/// Registration" Scope item. Carries raw primitives, not Domain Value Objects, across
/// the MediatR boundary -- the same choice every other framework's own commands in
/// this codebase already make.
/// </summary>
public sealed record CreateDocumentCommand(
    Guid TenantId,
    string Title,
    string Category,
    string Classification,
    Guid OwnerUserId,
    string? Description,
    string? DocumentNumber,
    Guid? CompanyId,
    Guid? DepartmentId,
    DateOnly? EffectiveDate,
    DateOnly? ExpirationDate,
    IReadOnlyList<string>? Tags) : ICommand<Result<Guid>>;

internal sealed class CreateDocumentCommandHandler : IRequestHandler<CreateDocumentCommand, Result<Guid>>
{
    private readonly IDocumentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CreateDocumentCommandHandler(IDocumentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(CreateDocumentCommand request, CancellationToken cancellationToken)
    {
        var classificationResult = DocumentManagementMapper.ParseClassification(request.Classification);
        if (classificationResult.IsFailure)
        {
            return Result.Failure<Guid>(classificationResult.Error);
        }

        var nowUtc = _timeProvider.GetUtcNow();
        var documentResult = Document.Create(
            request.TenantId,
            request.Title,
            request.Category,
            classificationResult.Value,
            request.OwnerUserId,
            request.Description,
            request.DocumentNumber,
            request.CompanyId,
            request.DepartmentId,
            request.EffectiveDate,
            request.ExpirationDate,
            request.Tags,
            nowUtc);

        if (documentResult.IsFailure)
        {
            return Result.Failure<Guid>(documentResult.Error);
        }

        await _repository.AddAsync(documentResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(documentResult.Value.Id.Value);
    }
}
