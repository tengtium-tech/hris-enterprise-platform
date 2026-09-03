using FluentAssertions;
using Hris.Foundation.Search.Domain;
using Xunit;

namespace Hris.Foundation.Search.Tests.Domain;

public sealed class SearchIndexDefinitionTests
{
    [Fact]
    public void Register_Succeeds_WithValidInput()
    {
        var entityType = TestData.NewEntityType("Employee");
        var fields = TestData.NewFields();

        var result = SearchIndexDefinition.Register(entityType, fields, "employee.read", TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.EntityType.Should().Be(entityType);
        result.Value.Fields.Should().BeEquivalentTo(fields);
        result.Value.SecurityScopeKey.Should().Be("employee.read");
        result.Value.RegisteredAtUtc.Should().Be(TestData.NowUtc);
        result.Value.LastRebuiltAtUtc.Should().BeNull();
        result.Value.DomainEvents.Should().BeEmpty("search-framework.md's own Domain Events list names no definition-registered event");
    }

    [Fact]
    public void Register_Fails_WhenNoFieldsProvided()
    {
        var result = SearchIndexDefinition.Register(TestData.NewEntityType(), [], null, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.NoFieldsProvided);
    }

    [Fact]
    public void Register_Fails_WhenNoFieldIsSearchable()
    {
        IReadOnlyList<SearchFieldDefinition> fields = [new SearchFieldDefinition("Department", false, true, true, 5)];

        var result = SearchIndexDefinition.Register(TestData.NewEntityType(), fields, null, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.NoSearchableFieldProvided);
    }

    [Fact]
    public void Register_Fails_WhenAFieldNameIsMissing()
    {
        IReadOnlyList<SearchFieldDefinition> fields = [new SearchFieldDefinition(" ", true, true, true, 5)];

        var result = SearchIndexDefinition.Register(TestData.NewEntityType(), fields, null, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.FieldNameRequired);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void Register_Fails_WhenAFieldWeightIsOutOfRange(int weight)
    {
        IReadOnlyList<SearchFieldDefinition> fields = [new SearchFieldDefinition("FullName", true, true, true, weight)];

        var result = SearchIndexDefinition.Register(TestData.NewEntityType(), fields, null, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.FieldWeightOutOfRange);
    }

    [Fact]
    public void Register_Fails_WhenFieldNamesAreDuplicated()
    {
        IReadOnlyList<SearchFieldDefinition> fields =
        [
            new SearchFieldDefinition("FullName", true, true, true, 5),
            new SearchFieldDefinition("fullname", true, true, true, 3),
        ];

        var result = SearchIndexDefinition.Register(TestData.NewEntityType(), fields, null, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.DuplicateFieldName);
    }

    [Fact]
    public void UpdateFields_Succeeds_AndReplacesTheFieldList()
    {
        var definition = TestData.RegisteredDefinition();
        IReadOnlyList<SearchFieldDefinition> newFields = [new SearchFieldDefinition("Email", true, false, false, 8)];

        var result = definition.UpdateFields(newFields, "employee.read.updated");

        result.IsSuccess.Should().BeTrue();
        definition.Fields.Should().BeEquivalentTo(newFields);
        definition.SecurityScopeKey.Should().Be("employee.read.updated");
    }

    [Fact]
    public void UpdateFields_Fails_WhenNoFieldsProvided()
    {
        var definition = TestData.RegisteredDefinition();

        var result = definition.UpdateFields([], null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.NoFieldsProvided);
    }

    [Fact]
    public void CompleteRebuild_Succeeds_AndRaisesSearchIndexRebuilt()
    {
        var definition = TestData.RegisteredDefinition();

        var result = definition.CompleteRebuild(1234, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        definition.LastRebuiltAtUtc.Should().Be(TestData.NowUtc);
        definition.DomainEvents.OfType<SearchIndexRebuilt>().Should().ContainSingle(e => e.DocumentCount == 1234);
    }

    [Fact]
    public void CompleteRebuild_Throws_WhenDocumentCountIsNegative()
    {
        var definition = TestData.RegisteredDefinition();

        var act = () => definition.CompleteRebuild(-1, TestData.NowUtc);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
