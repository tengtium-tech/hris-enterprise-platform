using FluentAssertions;
using Hris.Foundation.StatutoryReferenceData.Application.Queries;
using Hris.Foundation.StatutoryReferenceData.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.StatutoryReferenceData.Tests.Application;

public sealed class GetStatutoryProgramQueryHandlerTests
{
    private readonly IStatutoryProgramRepository _repository = Substitute.For<IStatutoryProgramRepository>();
    private readonly GetStatutoryProgramQueryHandler _handler;

    public GetStatutoryProgramQueryHandlerTests()
    {
        _handler = new GetStatutoryProgramQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_Succeeds_AndReturnsTheMappedDto()
    {
        var program = TestData.NewProgram();
        _repository.GetByIdAsync(program.Id, Arg.Any<CancellationToken>()).Returns(program);

        var result = await _handler.Handle(new GetStatutoryProgramQuery(program.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.StatutoryProgramId.Should().Be(program.Id.Value);
        result.Value.Code.Should().Be(program.Code.Value);
    }

    [Fact]
    public async Task Handle_Fails_WhenProgramDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<StatutoryProgramId>(), Arg.Any<CancellationToken>())
            .Returns((StatutoryProgram?)null);

        var result = await _handler.Handle(new GetStatutoryProgramQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StatutoryReferenceDataErrors.ProgramNotFound);
    }
}
