using FluentAssertions;
using Hris.Foundation.StatutoryReferenceData.Application.Commands;
using Hris.Foundation.StatutoryReferenceData.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.StatutoryReferenceData.Tests.Application;

public sealed class RecordStatutoryTableVersionSignoffCommandHandlerTests
{
    private readonly IStatutoryTableVersionRepository _repository = Substitute.For<IStatutoryTableVersionRepository>();
    private readonly RecordStatutoryTableVersionSignoffCommandHandler _handler;

    public RecordStatutoryTableVersionSignoffCommandHandlerTests()
    {
        _handler = new RecordStatutoryTableVersionSignoffCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenVersionExistsAndIsNotYetSignedOff()
    {
        var version = TestData.PublishedVersion();
        _repository.GetByIdAsync(version.Id, Arg.Any<CancellationToken>()).Returns(version);

        var result = await _handler.Handle(new RecordStatutoryTableVersionSignoffCommand(version.Id.Value, "Reviewer Name"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        version.Provenance.SignoffStatus.Should().Be(StatutorySignoffStatus.SignedOff);
    }

    [Fact]
    public async Task Handle_Fails_WhenVersionDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<StatutoryTableVersionId>(), Arg.Any<CancellationToken>())
            .Returns((StatutoryTableVersion?)null);

        var result = await _handler.Handle(
            new RecordStatutoryTableVersionSignoffCommand(Guid.NewGuid(), "Reviewer Name"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StatutoryReferenceDataErrors.StatutoryTableVersionNotFound);
    }

    [Fact]
    public async Task Handle_Fails_WhenAlreadySignedOff()
    {
        var version = TestData.SignedOffVersion();
        _repository.GetByIdAsync(version.Id, Arg.Any<CancellationToken>()).Returns(version);

        var result = await _handler.Handle(
            new RecordStatutoryTableVersionSignoffCommand(version.Id.Value, "Second Reviewer"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StatutoryReferenceDataErrors.AlreadySignedOff);
    }
}
