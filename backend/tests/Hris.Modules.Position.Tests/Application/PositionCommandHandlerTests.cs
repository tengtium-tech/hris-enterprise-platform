using FluentAssertions;
using Hris.Modules.Position.Application.Commands;
using Hris.Modules.Position.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Position.Tests.Application;

public sealed class CreatePositionCommandHandlerTests
{
    private readonly IPositionRepository _repository = Substitute.For<IPositionRepository>();
    private readonly CreatePositionCommandHandler _handler;

    public CreatePositionCommandHandlerTests()
    {
        _handler = new CreatePositionCommandHandler(_repository, new FakeTimeProvider(TestPosition.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenNumberIsUniqueAndDataIsValid()
    {
        var tenantId = Guid.NewGuid();
        _repository.ExistsWithNumberAsync(tenantId, "POS-000001", null, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(
            new CreatePositionCommand(
                tenantId, "POS-000001", "Senior Software Engineer", "Individual Contributor", Guid.NewGuid(), null,
                null, null, null, null, null, null, null, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 1),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<Hris.Modules.Position.Domain.Position>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenNumberAlreadyExists()
    {
        var tenantId = Guid.NewGuid();
        _repository.ExistsWithNumberAsync(tenantId, "POS-000001", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(
            new CreatePositionCommand(
                tenantId, "POS-000001", "Senior Software Engineer", "Individual Contributor", Guid.NewGuid(), null,
                null, null, null, null, null, null, null, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 1),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.DuplicatePositionNumber);
    }

    [Fact]
    public async Task Handle_Fails_WhenTitleIsMissing()
    {
        var tenantId = Guid.NewGuid();
        _repository.ExistsWithNumberAsync(tenantId, "POS-000001", null, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(
            new CreatePositionCommand(
                tenantId, "POS-000001", null, "Individual Contributor", Guid.NewGuid(), null, null, null, null, null,
                null, null, null, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 1),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.PositionTitleRequired);
    }
}

public sealed class UpdatePositionCommandHandlerTests
{
    private readonly IPositionRepository _repository = Substitute.For<IPositionRepository>();
    private readonly UpdatePositionCommandHandler _handler;

    public UpdatePositionCommandHandlerTests()
    {
        _handler = new UpdatePositionCommandHandler(_repository, new FakeTimeProvider(TestPosition.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenPositionExists()
    {
        var tenantId = Guid.NewGuid();
        var position = TestPosition.Create(tenantId);
        _repository.GetByIdAsync(position.Id, Arg.Any<CancellationToken>()).Returns(position);

        var result = await _handler.Handle(
            new UpdatePositionCommand(
                position.Id.Value, tenantId, "New Title", "New Type", Guid.NewGuid(), null, null, null, null, null,
                null, null, null, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        position.Title.Value.Should().Be("New Title");
    }

    [Fact]
    public async Task Handle_Fails_WhenPositionNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<PositionId>(), Arg.Any<CancellationToken>()).Returns((Hris.Modules.Position.Domain.Position?)null);

        var result = await _handler.Handle(
            new UpdatePositionCommand(
                Guid.NewGuid(), Guid.NewGuid(), "New Title", "New Type", Guid.NewGuid(), null, null, null, null, null,
                null, null, null, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.PositionNotFound);
    }

    [Fact]
    public async Task Handle_Fails_WhenTenantDoesNotMatch()
    {
        var position = TestPosition.Create(Guid.NewGuid());
        _repository.GetByIdAsync(position.Id, Arg.Any<CancellationToken>()).Returns(position);

        var result = await _handler.Handle(
            new UpdatePositionCommand(
                position.Id.Value, Guid.NewGuid(), "New Title", "New Type", Guid.NewGuid(), null, null, null, null,
                null, null, null, null, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.PositionNotFound);
    }
}

public sealed class ActivatePositionCommandHandlerTests
{
    private readonly IPositionRepository _repository = Substitute.For<IPositionRepository>();
    private readonly ActivatePositionCommandHandler _handler;

    public ActivatePositionCommandHandlerTests()
    {
        _handler = new ActivatePositionCommandHandler(_repository, new FakeTimeProvider(TestPosition.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenPositionExists()
    {
        var tenantId = Guid.NewGuid();
        var position = TestPosition.Create(tenantId);
        _repository.GetByIdAsync(position.Id, Arg.Any<CancellationToken>()).Returns(position);

        var result = await _handler.Handle(new ActivatePositionCommand(position.Id.Value, tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        position.Status.Should().Be(PositionStatus.Active);
    }

    [Fact]
    public async Task Handle_Fails_WhenNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<PositionId>(), Arg.Any<CancellationToken>()).Returns((Hris.Modules.Position.Domain.Position?)null);

        var result = await _handler.Handle(new ActivatePositionCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.PositionNotFound);
    }
}

public sealed class DeactivatePositionCommandHandlerTests
{
    private readonly IPositionRepository _repository = Substitute.For<IPositionRepository>();
    private readonly DeactivatePositionCommandHandler _handler;

    public DeactivatePositionCommandHandlerTests()
    {
        _handler = new DeactivatePositionCommandHandler(_repository, new FakeTimeProvider(TestPosition.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenPositionIsActive()
    {
        var tenantId = Guid.NewGuid();
        var position = TestPosition.Create(tenantId);
        position.Activate(TestPosition.NowUtc);
        _repository.GetByIdAsync(position.Id, Arg.Any<CancellationToken>()).Returns(position);

        var result = await _handler.Handle(new DeactivatePositionCommand(position.Id.Value, tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        position.Status.Should().Be(PositionStatus.Inactive);
    }

    [Fact]
    public async Task Handle_Fails_WhenNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<PositionId>(), Arg.Any<CancellationToken>()).Returns((Hris.Modules.Position.Domain.Position?)null);

        var result = await _handler.Handle(new DeactivatePositionCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.PositionNotFound);
    }
}

public sealed class ArchivePositionCommandHandlerTests
{
    private readonly IPositionRepository _repository = Substitute.For<IPositionRepository>();
    private readonly ArchivePositionCommandHandler _handler;

    public ArchivePositionCommandHandlerTests()
    {
        _handler = new ArchivePositionCommandHandler(_repository, new FakeTimeProvider(TestPosition.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenPositionIsActive()
    {
        var tenantId = Guid.NewGuid();
        var position = TestPosition.Create(tenantId);
        position.Activate(TestPosition.NowUtc);
        _repository.GetByIdAsync(position.Id, Arg.Any<CancellationToken>()).Returns(position);

        var result = await _handler.Handle(new ArchivePositionCommand(position.Id.Value, tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        position.Status.Should().Be(PositionStatus.Archived);
    }

    [Fact]
    public async Task Handle_Fails_WhenNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<PositionId>(), Arg.Any<CancellationToken>()).Returns((Hris.Modules.Position.Domain.Position?)null);

        var result = await _handler.Handle(new ArchivePositionCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.PositionNotFound);
    }
}
