using FluentAssertions;
using Hris.Modules.Employment.Application.Commands;
using Hris.Modules.Employment.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Employment.Tests.Application;

public sealed class AssignPositionCommandHandlerTests
{
    private readonly IEmploymentAssignmentRepository _repository = Substitute.For<IEmploymentAssignmentRepository>();
    private readonly AssignPositionCommandHandler _handler;

    public AssignPositionCommandHandlerTests()
    {
        _handler = new AssignPositionCommandHandler(_repository, new FakeTimeProvider(TestEmployment.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenPositionIsActive()
    {
        var result = await _handler.Handle(
            new AssignPositionCommand(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, null, null, null,
                WorkArrangement.OnSite, null, DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime), true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<EmploymentAssignment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenPositionIsNotActive()
    {
        var result = await _handler.Handle(
            new AssignPositionCommand(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, null, null, null, null, WorkArrangement.OnSite,
                null, DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime), false),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.AssignmentRequiresActivePosition);
    }
}

public sealed class TransferEmploymentCommandHandlerTests
{
    private readonly IEmploymentAssignmentRepository _repository = Substitute.For<IEmploymentAssignmentRepository>();
    private readonly TransferEmploymentCommandHandler _handler;

    public TransferEmploymentCommandHandlerTests()
    {
        _handler = new TransferEmploymentCommandHandler(_repository, new FakeTimeProvider(TestEmployment.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenAssignmentExists()
    {
        var tenantId = Guid.NewGuid();
        var assignment = TestEmployment.CreateAssignment(tenantId, Guid.NewGuid());
        _repository.GetByIdAsync(assignment.Id, Arg.Any<CancellationToken>()).Returns(assignment);
        var newPositionId = Guid.NewGuid();

        var result = await _handler.Handle(
            new TransferEmploymentCommand(
                assignment.Id.Value, tenantId, newPositionId, Guid.NewGuid(), null, null, null, null,
                WorkArrangement.Remote, DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime).AddDays(30), null, true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        assignment.PositionId.Should().Be(newPositionId);
    }

    [Fact]
    public async Task Handle_Fails_WhenAssignmentNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<EmploymentAssignmentId>(), Arg.Any<CancellationToken>())
            .Returns((EmploymentAssignment?)null);

        var result = await _handler.Handle(
            new TransferEmploymentCommand(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, null, null, null, null, WorkArrangement.OnSite,
                DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime), null, true),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentAssignmentNotFound);
    }
}

public sealed class PromoteEmploymentCommandHandlerTests
{
    private readonly IEmploymentAssignmentRepository _repository = Substitute.For<IEmploymentAssignmentRepository>();
    private readonly PromoteEmploymentCommandHandler _handler;

    public PromoteEmploymentCommandHandlerTests()
    {
        _handler = new PromoteEmploymentCommandHandler(_repository, new FakeTimeProvider(TestEmployment.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_RaisingEmploymentPromoted()
    {
        var tenantId = Guid.NewGuid();
        var assignment = TestEmployment.CreateAssignment(tenantId, Guid.NewGuid());
        _repository.GetByIdAsync(assignment.Id, Arg.Any<CancellationToken>()).Returns(assignment);

        var result = await _handler.Handle(
            new PromoteEmploymentCommand(
                assignment.Id.Value, tenantId, Guid.NewGuid(), null, null, null, null, null, WorkArrangement.OnSite,
                DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime).AddDays(30), "Approved", true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        assignment.DomainEvents.Should().Contain(e => e is EmploymentPromoted);
    }
}

public sealed class DemoteEmploymentCommandHandlerTests
{
    private readonly IEmploymentAssignmentRepository _repository = Substitute.For<IEmploymentAssignmentRepository>();
    private readonly DemoteEmploymentCommandHandler _handler;

    public DemoteEmploymentCommandHandlerTests()
    {
        _handler = new DemoteEmploymentCommandHandler(_repository, new FakeTimeProvider(TestEmployment.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_RaisingEmploymentDemoted()
    {
        var tenantId = Guid.NewGuid();
        var assignment = TestEmployment.CreateAssignment(tenantId, Guid.NewGuid());
        _repository.GetByIdAsync(assignment.Id, Arg.Any<CancellationToken>()).Returns(assignment);

        var result = await _handler.Handle(
            new DemoteEmploymentCommand(
                assignment.Id.Value, tenantId, Guid.NewGuid(), null, null, null, null, null, WorkArrangement.OnSite,
                DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime).AddDays(30), "Approved", true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        assignment.DomainEvents.Should().Contain(e => e is EmploymentDemoted);
    }
}

public sealed class ChangeReportingManagerCommandHandlerTests
{
    private readonly IEmploymentAssignmentRepository _repository = Substitute.For<IEmploymentAssignmentRepository>();
    private readonly ChangeReportingManagerCommandHandler _handler;

    public ChangeReportingManagerCommandHandlerTests()
    {
        _handler = new ChangeReportingManagerCommandHandler(_repository, new FakeTimeProvider(TestEmployment.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenNoCircularReporting()
    {
        var tenantId = Guid.NewGuid();
        var assignment = TestEmployment.CreateAssignment(tenantId, Guid.NewGuid());
        _repository.GetByIdAsync(assignment.Id, Arg.Any<CancellationToken>()).Returns(assignment);
        var newManagerId = Guid.NewGuid();
        _repository.WouldCreateCircularReportingAsync(tenantId, assignment.EmploymentId, newManagerId, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _handler.Handle(
            new ChangeReportingManagerCommand(
                assignment.Id.Value, tenantId, newManagerId, DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime).AddDays(1)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        assignment.ReportingManagerEmploymentId.Should().Be(newManagerId);
    }

    [Fact]
    public async Task Handle_Fails_WhenWouldCreateCircularReporting()
    {
        var tenantId = Guid.NewGuid();
        var assignment = TestEmployment.CreateAssignment(tenantId, Guid.NewGuid());
        _repository.GetByIdAsync(assignment.Id, Arg.Any<CancellationToken>()).Returns(assignment);
        var newManagerId = Guid.NewGuid();
        _repository.WouldCreateCircularReportingAsync(tenantId, assignment.EmploymentId, newManagerId, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _handler.Handle(
            new ChangeReportingManagerCommand(
                assignment.Id.Value, tenantId, newManagerId, DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime).AddDays(1)),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.CircularReportingProhibited);
    }
}

public sealed class EndAssignmentCommandHandlerTests
{
    private readonly IEmploymentAssignmentRepository _repository = Substitute.For<IEmploymentAssignmentRepository>();
    private readonly EndAssignmentCommandHandler _handler;

    public EndAssignmentCommandHandlerTests()
    {
        _handler = new EndAssignmentCommandHandler(_repository, new FakeTimeProvider(TestEmployment.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenAssignmentIsCurrent()
    {
        var tenantId = Guid.NewGuid();
        var assignment = TestEmployment.CreateAssignment(tenantId, Guid.NewGuid());
        _repository.GetByIdAsync(assignment.Id, Arg.Any<CancellationToken>()).Returns(assignment);

        var result = await _handler.Handle(
            new EndAssignmentCommand(assignment.Id.Value, tenantId, DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        assignment.IsEnded.Should().BeTrue();
    }
}
