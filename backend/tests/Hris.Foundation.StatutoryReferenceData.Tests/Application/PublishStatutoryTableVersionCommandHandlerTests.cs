using FluentAssertions;
using Hris.Foundation.StatutoryReferenceData.Application.Commands;
using Hris.Foundation.StatutoryReferenceData.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.StatutoryReferenceData.Tests.Application;

public sealed class PublishStatutoryTableVersionCommandHandlerTests
{
    private readonly IStatutoryProgramRepository _programRepository = Substitute.For<IStatutoryProgramRepository>();
    private readonly IStatutoryTableVersionRepository _versionRepository = Substitute.For<IStatutoryTableVersionRepository>();
    private readonly PublishStatutoryTableVersionCommandHandler _handler;
    private readonly StatutoryProgram _program = TestData.NewProgram();

    public PublishStatutoryTableVersionCommandHandlerTests()
    {
        _handler = new PublishStatutoryTableVersionCommandHandler(
            _programRepository, _versionRepository, new FakeTimeProvider(TestData.NowUtc));

        _programRepository.GetByIdAsync(Arg.Any<StatutoryProgramId>(), Arg.Any<CancellationToken>()).Returns(_program);
    }

    private PublishStatutoryTableVersionCommand ValidCommand() => new(
        _program.Id.Value,
        "2025-01",
        TestData.NowUtc,
        null,
        "Social Security System (SSS)",
        "SSS Circular No. 2024-006",
        TestData.NowUtc,
        StatutoryVerificationSourceType.PrimarySourceRead,
        TestData.NowUtc,
        TestData.NewScheduleData());

    [Fact]
    public async Task Handle_Succeeds_AndPersistsTheNewVersion()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _versionRepository.Received(1).AddAsync(Arg.Any<StatutoryTableVersion>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenProgramDoesNotExist()
    {
        _programRepository.GetByIdAsync(Arg.Any<StatutoryProgramId>(), Arg.Any<CancellationToken>())
            .Returns((StatutoryProgram?)null);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StatutoryReferenceDataErrors.ProgramNotFound);
        await _versionRepository.DidNotReceive().AddAsync(Arg.Any<StatutoryTableVersion>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenVersionLabelIsInvalid()
    {
        var result = await _handler.Handle(ValidCommand() with { VersionLabel = "not-a-label" }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StatutoryReferenceDataErrors.VersionLabelInvalidFormat);
    }

    [Fact]
    public async Task Handle_Fails_WhenVersionLabelAlreadyExistsForProgram()
    {
        _versionRepository.ExistsByProgramAndVersionLabelAsync(
                Arg.Any<StatutoryProgramId>(), Arg.Any<StatutoryTableVersionLabel>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StatutoryReferenceDataErrors.DuplicateVersionLabel);
        await _versionRepository.DidNotReceive().AddAsync(Arg.Any<StatutoryTableVersion>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenScheduleDataIsNotValidJson()
    {
        var result = await _handler.Handle(ValidCommand() with { ScheduleData = "{invalid" }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StatutoryReferenceDataErrors.ScheduleDataMustBeValidJson);
    }
}
