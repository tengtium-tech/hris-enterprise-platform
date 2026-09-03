using FluentAssertions;
using Hris.Foundation.Search.Application.Queries;
using Hris.Foundation.Search.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Search.Tests.Application;

public sealed class GlobalSearchQueryHandlerTests
{
    private readonly ISearchIndexDefinitionRepository _definitionRepository = Substitute.For<ISearchIndexDefinitionRepository>();
    private readonly IIndexedDocumentRepository _documentRepository = Substitute.For<IIndexedDocumentRepository>();
    private readonly ISearchExecutionRepository _executionRepository = Substitute.For<ISearchExecutionRepository>();
    private readonly GlobalSearchQueryHandler _handler;

    public GlobalSearchQueryHandlerTests()
    {
        _handler = new GlobalSearchQueryHandler(
            _definitionRepository, _documentRepository, _executionRepository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_AndGroupsHitsByDomain_WhenNoDomainFilterIsGiven()
    {
        var hits = new[]
        {
            new IndexedDocumentSearchHit(new IndexedDocumentId(Guid.NewGuid()), TestData.NewEntityType("Employee"), "employee-0001", "Juan Dela Cruz", 0.9),
            new IndexedDocumentSearchHit(new IndexedDocumentId(Guid.NewGuid()), TestData.NewEntityType("Payroll"), "payroll-0001", "Juan Dela Cruz payslip", 0.5),
        };
        _documentRepository
            .SearchAsync(TestData.TenantId, "Juan", null, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(hits);

        var query = new GlobalSearchQuery(TestData.TenantId, TestData.UserId, "Juan", null, []);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalResultCount.Should().Be(2);
        result.Value.Groups.Should().HaveCount(2);
        result.Value.Groups.Should().Contain(g => g.Domain == "EMPLOYEE");
        result.Value.Groups.Should().Contain(g => g.Domain == "PAYROLL");

        await _executionRepository.Received(1).AddAsync(
            Arg.Is<SearchExecution>(e => e.Status == SearchExecutionStatus.Completed && e.ResultCount == 2), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesTheDomainFilterAndScopeTokensThroughToTheRepository()
    {
        _definitionRepository.ExistsByEntityTypeAsync(Arg.Any<SearchEntityType>(), Arg.Any<CancellationToken>()).Returns(true);
        _documentRepository
            .SearchAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<SearchEntityType>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var scopeTokens = new[] { "employee.read" };
        var query = new GlobalSearchQuery(TestData.TenantId, TestData.UserId, "Juan", "Employee", scopeTokens);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _documentRepository.Received(1).SearchAsync(
            TestData.TenantId,
            "Juan",
            Arg.Is<SearchEntityType>(t => t.Value == "EMPLOYEE"),
            scopeTokens,
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_AndRecordsSearchFailed_WhenDomainFilterHasNoRegisteredDefinition()
    {
        _definitionRepository.ExistsByEntityTypeAsync(Arg.Any<SearchEntityType>(), Arg.Any<CancellationToken>()).Returns(false);

        var query = new GlobalSearchQuery(TestData.TenantId, TestData.UserId, "Juan", "Unregistered", []);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.SearchIndexDefinitionNotFound);
        await _executionRepository.Received(1).AddAsync(
            Arg.Is<SearchExecution>(e => e.Status == SearchExecutionStatus.Failed), Arg.Any<CancellationToken>());
        await _documentRepository.DidNotReceive().SearchAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<SearchEntityType?>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenQueryTextIsMissing_WithoutRecordingAnExecution()
    {
        var query = new GlobalSearchQuery(TestData.TenantId, TestData.UserId, " ", null, []);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.QueryTextRequired);
        await _executionRepository.DidNotReceive().AddAsync(Arg.Any<SearchExecution>(), Arg.Any<CancellationToken>());
    }
}
