using FluentAssertions;
using Hris.Foundation.StatutoryReferenceData.Application.Queries;
using Hris.Foundation.StatutoryReferenceData.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.StatutoryReferenceData.Tests.Application;

public sealed class GetEffectiveStatutoryTableVersionQueryHandlerTests
{
    private readonly IStatutoryProgramRepository _programRepository = Substitute.For<IStatutoryProgramRepository>();
    private readonly IStatutoryTableVersionRepository _versionRepository = Substitute.For<IStatutoryTableVersionRepository>();
    private readonly GetEffectiveStatutoryTableVersionQueryHandler _handler;
    private readonly StatutoryProgram _program = TestData.NewProgram();

    public GetEffectiveStatutoryTableVersionQueryHandlerTests()
    {
        _handler = new GetEffectiveStatutoryTableVersionQueryHandler(_programRepository, _versionRepository);
        _programRepository.GetByCodeAndCountryAsync(Arg.Any<StatutoryProgramCode>(), Arg.Any<StatutoryCountryCode>(), Arg.Any<CancellationToken>())
            .Returns(_program);
    }

    [Fact]
    public async Task Handle_Succeeds_WhenAnEffectiveSignedOffVersionExists()
    {
        var version = TestData.SignedOffVersion(_program.Id);
        _versionRepository.GetLatestEffectiveAsOfAsync(_program.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(version);

        var result = await _handler.Handle(
            new GetEffectiveStatutoryTableVersionQuery("SSS", "PH", TestData.NowUtc), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.StatutoryTableVersionId.Should().Be(version.Id.Value);
    }

    [Fact]
    public async Task Handle_Fails_WhenProgramDoesNotExist()
    {
        _programRepository.GetByCodeAndCountryAsync(Arg.Any<StatutoryProgramCode>(), Arg.Any<StatutoryCountryCode>(), Arg.Any<CancellationToken>())
            .Returns((StatutoryProgram?)null);

        var result = await _handler.Handle(
            new GetEffectiveStatutoryTableVersionQuery("SSS", "PH", TestData.NowUtc), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StatutoryReferenceDataErrors.ProgramNotFound);
    }

    [Fact]
    public async Task Handle_Fails_WhenNoVersionIsEffectiveForThePeriod()
    {
        _versionRepository.GetLatestEffectiveAsOfAsync(_program.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns((StatutoryTableVersion?)null);

        var result = await _handler.Handle(
            new GetEffectiveStatutoryTableVersionQuery("SSS", "PH", TestData.NowUtc), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StatutoryReferenceDataErrors.NoApplicableTableForPeriod);
    }

    [Fact]
    public async Task Handle_Fails_WhenTheEffectiveVersionIsNotYetSignedOff()
    {
        var version = TestData.PublishedVersion(_program.Id);
        _versionRepository.GetLatestEffectiveAsOfAsync(_program.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(version);

        var result = await _handler.Handle(
            new GetEffectiveStatutoryTableVersionQuery("SSS", "PH", TestData.NowUtc), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StatutoryReferenceDataErrors.NoSignedOffApplicableTableForPeriod);
    }

    [Fact]
    public async Task Handle_Fails_WhenProgramCodeIsInvalid()
    {
        var result = await _handler.Handle(
            new GetEffectiveStatutoryTableVersionQuery("bad code", "PH", TestData.NowUtc), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StatutoryReferenceDataErrors.ProgramCodeInvalidFormat);
    }
}
