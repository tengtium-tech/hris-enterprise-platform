using FluentAssertions;
using Hris.Foundation.StatutoryReferenceData.Application.Commands;
using Hris.Foundation.StatutoryReferenceData.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.StatutoryReferenceData.Tests.Application;

public sealed class RegisterStatutoryProgramCommandHandlerTests
{
    private readonly IStatutoryProgramRepository _repository = Substitute.For<IStatutoryProgramRepository>();
    private readonly RegisterStatutoryProgramCommandHandler _handler;

    public RegisterStatutoryProgramCommandHandlerTests()
    {
        _handler = new RegisterStatutoryProgramCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    private static RegisterStatutoryProgramCommand ValidCommand() => new("SSS", "PH", "SSS Contribution Schedule");

    [Fact]
    public async Task Handle_Succeeds_AndPersistsTheNewProgram()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<StatutoryProgram>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenCodeIsInvalid_WithoutCallingTheRepository()
    {
        var result = await _handler.Handle(ValidCommand() with { Code = "bad code" }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StatutoryReferenceDataErrors.ProgramCodeInvalidFormat);
        await _repository.DidNotReceive().AddAsync(Arg.Any<StatutoryProgram>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenCountryIsInvalid()
    {
        var result = await _handler.Handle(ValidCommand() with { Country = "ZZ" }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StatutoryReferenceDataErrors.CountryCodeInvalidFormat);
    }

    [Fact]
    public async Task Handle_Fails_WhenCodeAlreadyExistsForCountry()
    {
        _repository.ExistsByCodeAndCountryAsync(Arg.Any<StatutoryProgramCode>(), Arg.Any<StatutoryCountryCode>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StatutoryReferenceDataErrors.DuplicateProgramCode);
        await _repository.DidNotReceive().AddAsync(Arg.Any<StatutoryProgram>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenDisplayNameIsMissing()
    {
        var result = await _handler.Handle(ValidCommand() with { DisplayName = string.Empty }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StatutoryReferenceDataErrors.DisplayNameRequired);
    }
}
