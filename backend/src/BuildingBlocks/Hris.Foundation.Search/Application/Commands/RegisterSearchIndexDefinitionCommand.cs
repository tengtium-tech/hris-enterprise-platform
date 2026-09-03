using Hris.Application.Abstractions;
using Hris.Foundation.Search.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Search.Application.Commands;

/// <summary>
/// Registers a new search index definition -- the entry point a business module calls
/// once, at startup or first use, to declare one of its own entity types searchable.
/// Carries raw primitives, not Domain Value Objects, across the MediatR boundary --
/// this handler is the one place a malformed entity type or field list becomes a
/// <see cref="SearchErrors"/> failure.
/// </summary>
public sealed record RegisterSearchIndexDefinitionCommand(
    string EntityType,
    IReadOnlyList<SearchFieldDefinition> Fields,
    string? SecurityScopeKey) : ICommand<Result<Guid>>;

internal sealed class RegisterSearchIndexDefinitionCommandHandler : IRequestHandler<RegisterSearchIndexDefinitionCommand, Result<Guid>>
{
    private readonly ISearchIndexDefinitionRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RegisterSearchIndexDefinitionCommandHandler(ISearchIndexDefinitionRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(RegisterSearchIndexDefinitionCommand request, CancellationToken cancellationToken)
    {
        var entityTypeResult = SearchEntityType.Create(request.EntityType);
        if (entityTypeResult.IsFailure)
        {
            return Result.Failure<Guid>(entityTypeResult.Error);
        }

        if (await _repository.ExistsByEntityTypeAsync(entityTypeResult.Value, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure<Guid>(SearchErrors.SearchIndexDefinitionAlreadyRegistered);
        }

        var registrationResult = SearchIndexDefinition.Register(
            entityTypeResult.Value, request.Fields, request.SecurityScopeKey, _timeProvider.GetUtcNow());
        if (registrationResult.IsFailure)
        {
            return Result.Failure<Guid>(registrationResult.Error);
        }

        await _repository.AddAsync(registrationResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(registrationResult.Value.Id.Value);
    }
}
