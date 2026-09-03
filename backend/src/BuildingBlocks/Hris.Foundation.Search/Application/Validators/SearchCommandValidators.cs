using FluentValidation;
using Hris.Foundation.Search.Application.Commands;
using Hris.Foundation.Search.Application.Queries;

namespace Hris.Foundation.Search.Application.Validators;

/// <summary>
/// application-pipeline.md's Validation Behavior scope: "Required fields...
/// Business-independent validation." Deliberately does not re-check anything the
/// Domain layer's own factory/transition methods already enforce (entity-type shape,
/// field-list invariants, lifecycle-state gating) -- the identical separation every
/// other framework's own validators file states for its own set.
/// </summary>
public sealed class RegisterSearchIndexDefinitionCommandValidator : AbstractValidator<RegisterSearchIndexDefinitionCommand>
{
    public RegisterSearchIndexDefinitionCommandValidator()
    {
        RuleFor(c => c.EntityType).NotEmpty();
        RuleFor(c => c.Fields).NotEmpty();
    }
}

public sealed class UpdateSearchIndexFieldsCommandValidator : AbstractValidator<UpdateSearchIndexFieldsCommand>
{
    public UpdateSearchIndexFieldsCommandValidator()
    {
        RuleFor(c => c.SearchIndexDefinitionId).NotEmpty();
        RuleFor(c => c.Fields).NotEmpty();
    }
}

public sealed class CompleteIndexRebuildCommandValidator : AbstractValidator<CompleteIndexRebuildCommand>
{
    public CompleteIndexRebuildCommandValidator()
    {
        RuleFor(c => c.SearchIndexDefinitionId).NotEmpty();
        RuleFor(c => c.DocumentCount).GreaterThanOrEqualTo(0);
    }
}

public sealed class IndexDocumentCommandValidator : AbstractValidator<IndexDocumentCommand>
{
    public IndexDocumentCommandValidator()
    {
        RuleFor(c => c.SourceEntityType).NotEmpty();
        RuleFor(c => c.SourceEntityId).NotEmpty();
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.SearchableContent).NotEmpty();
    }
}

public sealed class RemoveIndexedDocumentCommandValidator : AbstractValidator<RemoveIndexedDocumentCommand>
{
    public RemoveIndexedDocumentCommandValidator()
    {
        RuleFor(c => c.IndexedDocumentId).NotEmpty();
    }
}

public sealed class SaveSearchCommandValidator : AbstractValidator<SaveSearchCommand>
{
    public SaveSearchCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.OwnerUserId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.QueryText).NotEmpty();
    }
}

public sealed class RenameSavedSearchCommandValidator : AbstractValidator<RenameSavedSearchCommand>
{
    public RenameSavedSearchCommandValidator()
    {
        RuleFor(c => c.SavedSearchId).NotEmpty();
        RuleFor(c => c.NewName).NotEmpty();
    }
}

public sealed class DeleteSavedSearchCommandValidator : AbstractValidator<DeleteSavedSearchCommand>
{
    public DeleteSavedSearchCommandValidator()
    {
        RuleFor(c => c.SavedSearchId).NotEmpty();
    }
}

public sealed class GetSearchIndexDefinitionQueryValidator : AbstractValidator<GetSearchIndexDefinitionQuery>
{
    public GetSearchIndexDefinitionQueryValidator()
    {
        RuleFor(q => q.EntityType).NotEmpty();
    }
}

public sealed class GlobalSearchQueryValidator : AbstractValidator<GlobalSearchQuery>
{
    public GlobalSearchQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.QueryText).NotEmpty();
    }
}

public sealed class ListSavedSearchesQueryValidator : AbstractValidator<ListSavedSearchesQuery>
{
    public ListSavedSearchesQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.OwnerUserId).NotEmpty();
    }
}

public sealed class GetSearchSuggestionsQueryValidator : AbstractValidator<GetSearchSuggestionsQuery>
{
    public GetSearchSuggestionsQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.OwnerUserId).NotEmpty();
    }
}
