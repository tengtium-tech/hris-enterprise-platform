using FluentAssertions;
using Hris.Modules.Position.Application.Commands;
using Hris.Modules.Position.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Position.Tests.Application;

public sealed class AssignReportingPositionCommandHandlerTests
{
    private readonly IPositionRepository _repository = Substitute.For<IPositionRepository>();
    private readonly AssignReportingPositionCommandHandler _handler;

    public AssignReportingPositionCommandHandlerTests()
    {
        _handler = new AssignReportingPositionCommandHandler(_repository, new FakeTimeProvider(TestPosition.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenReportingPositionIsActiveAndNoCycle()
    {
        var tenantId = Guid.NewGuid();
        var position = TestPosition.Create(tenantId);
        var reportingPosition = TestPosition.Create(tenantId);
        reportingPosition.Activate(TestPosition.NowUtc);
        _repository.GetByIdAsync(position.Id, Arg.Any<CancellationToken>()).Returns(position);
        _repository.GetByIdAsync(reportingPosition.Id, Arg.Any<CancellationToken>()).Returns(reportingPosition);
        _repository.WouldCreateCircularReportingAsync(tenantId, position.Id.Value, reportingPosition.Id.Value, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _handler.Handle(
            new AssignReportingPositionCommand(position.Id.Value, tenantId, reportingPosition.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        position.ReportingPositionId.Should().Be(reportingPosition.Id.Value);
    }

    [Fact]
    public async Task Handle_Fails_WhenReportingPositionNotFound()
    {
        var tenantId = Guid.NewGuid();
        var position = TestPosition.Create(tenantId);
        _repository.GetByIdAsync(position.Id, Arg.Any<CancellationToken>()).Returns(position);
        _repository.GetByIdAsync(Arg.Is<PositionId>(id => id != position.Id), Arg.Any<CancellationToken>())
            .Returns((Hris.Modules.Position.Domain.Position?)null);

        var result = await _handler.Handle(
            new AssignReportingPositionCommand(position.Id.Value, tenantId, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.ReportingPositionNotFound);
    }

    [Fact]
    public async Task Handle_Fails_WhenWouldCreateCircularReporting()
    {
        var tenantId = Guid.NewGuid();
        var position = TestPosition.Create(tenantId);
        var reportingPosition = TestPosition.Create(tenantId);
        reportingPosition.Activate(TestPosition.NowUtc);
        _repository.GetByIdAsync(position.Id, Arg.Any<CancellationToken>()).Returns(position);
        _repository.GetByIdAsync(reportingPosition.Id, Arg.Any<CancellationToken>()).Returns(reportingPosition);
        _repository.WouldCreateCircularReportingAsync(tenantId, position.Id.Value, reportingPosition.Id.Value, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _handler.Handle(
            new AssignReportingPositionCommand(position.Id.Value, tenantId, reportingPosition.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.CircularReportingProhibited);
    }

    [Fact]
    public async Task Handle_Fails_WhenPositionNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<PositionId>(), Arg.Any<CancellationToken>()).Returns((Hris.Modules.Position.Domain.Position?)null);

        var result = await _handler.Handle(
            new AssignReportingPositionCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.PositionNotFound);
    }
}

public sealed class RemoveReportingPositionCommandHandlerTests
{
    private readonly IPositionRepository _repository = Substitute.For<IPositionRepository>();
    private readonly RemoveReportingPositionCommandHandler _handler;

    public RemoveReportingPositionCommandHandlerTests()
    {
        _handler = new RemoveReportingPositionCommandHandler(_repository, new FakeTimeProvider(TestPosition.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenReportingPositionAssigned()
    {
        var tenantId = Guid.NewGuid();
        var position = TestPosition.Create(tenantId);
        position.AssignReportingPosition(Guid.NewGuid(), true, true, false, TestPosition.NowUtc);
        _repository.GetByIdAsync(position.Id, Arg.Any<CancellationToken>()).Returns(position);

        var result = await _handler.Handle(new RemoveReportingPositionCommand(position.Id.Value, tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        position.ReportingPositionId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Fails_WhenNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<PositionId>(), Arg.Any<CancellationToken>()).Returns((Hris.Modules.Position.Domain.Position?)null);

        var result = await _handler.Handle(new RemoveReportingPositionCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.PositionNotFound);
    }
}

public sealed class UpdateAuthorizedHeadcountCommandHandlerTests
{
    private readonly IPositionRepository _repository = Substitute.For<IPositionRepository>();
    private readonly UpdateAuthorizedHeadcountCommandHandler _handler;

    public UpdateAuthorizedHeadcountCommandHandlerTests()
    {
        _handler = new UpdateAuthorizedHeadcountCommandHandler(_repository, new FakeTimeProvider(TestPosition.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenPositionExists()
    {
        var tenantId = Guid.NewGuid();
        var position = TestPosition.Create(tenantId);
        _repository.GetByIdAsync(position.Id, Arg.Any<CancellationToken>()).Returns(position);

        var result = await _handler.Handle(new UpdateAuthorizedHeadcountCommand(position.Id.Value, tenantId, 10), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        position.AuthorizedHeadcount.Value.Should().Be(10);
    }

    [Fact]
    public async Task Handle_Fails_WhenNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<PositionId>(), Arg.Any<CancellationToken>()).Returns((Hris.Modules.Position.Domain.Position?)null);

        var result = await _handler.Handle(
            new UpdateAuthorizedHeadcountCommand(Guid.NewGuid(), Guid.NewGuid(), 10), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.PositionNotFound);
    }
}

public sealed class MarkPositionVacantCommandHandlerTests
{
    private readonly IPositionRepository _repository = Substitute.For<IPositionRepository>();
    private readonly MarkPositionVacantCommandHandler _handler;

    public MarkPositionVacantCommandHandlerTests()
    {
        _handler = new MarkPositionVacantCommandHandler(_repository, new FakeTimeProvider(TestPosition.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenPositionIsFilled()
    {
        var tenantId = Guid.NewGuid();
        var position = TestPosition.Create(tenantId);
        position.MarkFilled(TestPosition.NowUtc);
        _repository.GetByIdAsync(position.Id, Arg.Any<CancellationToken>()).Returns(position);

        var result = await _handler.Handle(new MarkPositionVacantCommand(position.Id.Value, tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        position.VacancyStatus.Should().Be(VacancyStatus.Vacant);
    }

    [Fact]
    public async Task Handle_Fails_WhenNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<PositionId>(), Arg.Any<CancellationToken>()).Returns((Hris.Modules.Position.Domain.Position?)null);

        var result = await _handler.Handle(new MarkPositionVacantCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.PositionNotFound);
    }
}

public sealed class MarkPositionFilledCommandHandlerTests
{
    private readonly IPositionRepository _repository = Substitute.For<IPositionRepository>();
    private readonly MarkPositionFilledCommandHandler _handler;

    public MarkPositionFilledCommandHandlerTests()
    {
        _handler = new MarkPositionFilledCommandHandler(_repository, new FakeTimeProvider(TestPosition.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenPositionIsVacant()
    {
        var tenantId = Guid.NewGuid();
        var position = TestPosition.Create(tenantId);
        _repository.GetByIdAsync(position.Id, Arg.Any<CancellationToken>()).Returns(position);

        var result = await _handler.Handle(new MarkPositionFilledCommand(position.Id.Value, tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        position.VacancyStatus.Should().Be(VacancyStatus.Filled);
    }

    [Fact]
    public async Task Handle_Fails_WhenNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<PositionId>(), Arg.Any<CancellationToken>()).Returns((Hris.Modules.Position.Domain.Position?)null);

        var result = await _handler.Handle(new MarkPositionFilledCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.PositionNotFound);
    }
}
