using FluentAssertions;
using Hris.Foundation.Search.Application.Commands;
using Hris.Foundation.Search.Application.Queries;
using Hris.Foundation.Search.Application.Validators;
using Xunit;

namespace Hris.Foundation.Search.Tests.Application;

/// <summary>
/// One valid-passes/invalid-fails pair per validator, the identical shape
/// <c>NumberingCommandValidatorsTests</c> already establishes.
/// </summary>
public sealed class SearchCommandValidatorsTests
{
    [Fact]
    public void RegisterSearchIndexDefinitionCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyEntityType()
    {
        var validator = new RegisterSearchIndexDefinitionCommandValidator();
        var valid = new RegisterSearchIndexDefinitionCommand("Employee", TestData.NewFields(), null);
        var invalid = valid with { EntityType = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateSearchIndexFieldsCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new UpdateSearchIndexFieldsCommandValidator();
        var valid = new UpdateSearchIndexFieldsCommand(Guid.NewGuid(), TestData.NewFields(), null);
        var invalid = valid with { SearchIndexDefinitionId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CompleteIndexRebuildCommandValidator_AcceptsAValidCommand_AndRejectsANegativeDocumentCount()
    {
        var validator = new CompleteIndexRebuildCommandValidator();
        var valid = new CompleteIndexRebuildCommand(Guid.NewGuid(), 0);
        var invalid = valid with { DocumentCount = -1 };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void IndexDocumentCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyTenantId()
    {
        var validator = new IndexDocumentCommandValidator();
        var valid = new IndexDocumentCommand("Employee", "employee-0001", Guid.NewGuid(), "content", null);
        var invalid = valid with { TenantId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RemoveIndexedDocumentCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new RemoveIndexedDocumentCommandValidator();

        validator.Validate(new RemoveIndexedDocumentCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new RemoveIndexedDocumentCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void SaveSearchCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyName()
    {
        var validator = new SaveSearchCommandValidator();
        var valid = new SaveSearchCommand(Guid.NewGuid(), Guid.NewGuid(), "My employees", "Engineer", null);
        var invalid = valid with { Name = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RenameSavedSearchCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyName()
    {
        var validator = new RenameSavedSearchCommandValidator();
        var valid = new RenameSavedSearchCommand(Guid.NewGuid(), "Renamed");
        var invalid = valid with { NewName = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void DeleteSavedSearchCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new DeleteSavedSearchCommandValidator();

        validator.Validate(new DeleteSavedSearchCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new DeleteSavedSearchCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetSearchIndexDefinitionQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyEntityType()
    {
        var validator = new GetSearchIndexDefinitionQueryValidator();

        validator.Validate(new GetSearchIndexDefinitionQuery("Employee")).IsValid.Should().BeTrue();
        validator.Validate(new GetSearchIndexDefinitionQuery(string.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GlobalSearchQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyQueryText()
    {
        var validator = new GlobalSearchQueryValidator();
        var valid = new GlobalSearchQuery(Guid.NewGuid(), Guid.NewGuid(), "Juan", null, []);
        var invalid = valid with { QueryText = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ListSavedSearchesQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyOwnerUserId()
    {
        var validator = new ListSavedSearchesQueryValidator();
        var valid = new ListSavedSearchesQuery(Guid.NewGuid(), Guid.NewGuid());
        var invalid = valid with { OwnerUserId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetSearchSuggestionsQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyTenantId()
    {
        var validator = new GetSearchSuggestionsQueryValidator();
        var valid = new GetSearchSuggestionsQuery(Guid.NewGuid(), Guid.NewGuid());
        var invalid = valid with { TenantId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }
}
