using FluentAssertions;
using Hris.Foundation.StatutoryReferenceData.Application.Queries;
using Hris.Foundation.StatutoryReferenceData.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.StatutoryReferenceData.Tests.Application;

public sealed class ListStatutoryTableVersionHistoryQueryHandlerTests
{
    private readonly IStatutoryTableVersionRepository _repository = Substitute.For<IStatutoryTableVersionRepository>();
    private readonly ListStatutoryTableVersionHistoryQueryHandler _handler;

    public ListStatutoryTableVersionHistoryQueryHandlerTests()
    {
        _handler = new ListStatutoryTableVersionHistoryQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_Succeeds_AndReturnsMappedDtos()
    {
        var programId = new StatutoryProgramId(Guid.NewGuid());
        var versions = new List<StatutoryTableVersion> { TestData.PublishedVersion(programId) };
        _repository.ListByProgramAsync(programId, Arg.Any<CancellationToken>()).Returns(versions);

        var result = await _handler.Handle(new ListStatutoryTableVersionHistoryQuery(programId.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }
}
