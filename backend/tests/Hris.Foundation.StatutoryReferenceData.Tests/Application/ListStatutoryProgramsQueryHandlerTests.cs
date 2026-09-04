using FluentAssertions;
using Hris.Foundation.StatutoryReferenceData.Application.Queries;
using Hris.Foundation.StatutoryReferenceData.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.StatutoryReferenceData.Tests.Application;

public sealed class ListStatutoryProgramsQueryHandlerTests
{
    private readonly IStatutoryProgramRepository _repository = Substitute.For<IStatutoryProgramRepository>();
    private readonly ListStatutoryProgramsQueryHandler _handler;

    public ListStatutoryProgramsQueryHandlerTests()
    {
        _handler = new ListStatutoryProgramsQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_Succeeds_AndReturnsMappedDtos()
    {
        var country = TestData.NewCountry();
        var programs = new List<StatutoryProgram> { TestData.NewProgram(country: country) };
        _repository.ListByCountryAsync(country, Arg.Any<CancellationToken>()).Returns(programs);

        var result = await _handler.Handle(new ListStatutoryProgramsQuery("PH"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_Fails_WhenCountryIsInvalid()
    {
        var result = await _handler.Handle(new ListStatutoryProgramsQuery("ZZ"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StatutoryReferenceDataErrors.CountryCodeInvalidFormat);
    }
}
