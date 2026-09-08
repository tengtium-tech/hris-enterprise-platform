using FluentAssertions;
using Hris.Modules.Employment.Application.Commands;
using Hris.Modules.Employment.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Employment.Tests.Application;

public sealed class SuspendEmploymentCommandHandlerTests
{
    private readonly IEmploymentRepository _repository = Substitute.For<IEmploymentRepository>();
    private readonly SuspendEmploymentCommandHandler _handler;

    public SuspendEmploymentCommandHandlerTests()
    {
        _handler = new SuspendEmploymentCommandHandler(_repository, new FakeTimeProvider(TestEmployment.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenEmploymentIsActive()
    {
        var tenantId = Guid.NewGuid();
        var employment = TestEmployment.CreateActive(tenantId);
        _repository.GetByIdAsync(employment.Id, Arg.Any<CancellationToken>()).Returns(employment);

        var result = await _handler.Handle(
            new SuspendEmploymentCommand(employment.Id.Value, tenantId, "Reason", DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employment.OperationalStatus.Should().Be(OperationalStatus.Suspended);
    }
}

public sealed class ReinstateEmploymentCommandHandlerTests
{
    private readonly IEmploymentRepository _repository = Substitute.For<IEmploymentRepository>();
    private readonly ReinstateEmploymentCommandHandler _handler;

    public ReinstateEmploymentCommandHandlerTests()
    {
        _handler = new ReinstateEmploymentCommandHandler(_repository, new FakeTimeProvider(TestEmployment.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenEmploymentIsSuspended()
    {
        var tenantId = Guid.NewGuid();
        var employment = TestEmployment.CreateActive(tenantId);
        var date = DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime);
        employment.Suspend("Reason", date, TestEmployment.NowUtc);
        _repository.GetByIdAsync(employment.Id, Arg.Any<CancellationToken>()).Returns(employment);

        var result = await _handler.Handle(new ReinstateEmploymentCommand(employment.Id.Value, tenantId, date), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employment.OperationalStatus.Should().Be(OperationalStatus.Active);
    }
}

public sealed class SecondEmploymentCommandHandlerTests
{
    private readonly IEmploymentRepository _repository = Substitute.For<IEmploymentRepository>();
    private readonly SecondEmploymentCommandHandler _handler;

    public SecondEmploymentCommandHandlerTests()
    {
        _handler = new SecondEmploymentCommandHandler(_repository, new FakeTimeProvider(TestEmployment.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenEmploymentIsActive()
    {
        var tenantId = Guid.NewGuid();
        var employment = TestEmployment.CreateActive(tenantId);
        _repository.GetByIdAsync(employment.Id, Arg.Any<CancellationToken>()).Returns(employment);

        var result = await _handler.Handle(
            new SecondEmploymentCommand(employment.Id.Value, tenantId, DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employment.OperationalStatus.Should().Be(OperationalStatus.Seconded);
    }
}

public sealed class SeparateEmploymentCommandHandlerTests
{
    private readonly IEmploymentRepository _repository = Substitute.For<IEmploymentRepository>();
    private readonly SeparateEmploymentCommandHandler _handler;

    public SeparateEmploymentCommandHandlerTests()
    {
        _handler = new SeparateEmploymentCommandHandler(_repository, new FakeTimeProvider(TestEmployment.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenEmploymentIsActive()
    {
        var tenantId = Guid.NewGuid();
        var employment = TestEmployment.CreateActive(tenantId);
        var date = DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime);
        _repository.GetByIdAsync(employment.Id, Arg.Any<CancellationToken>()).Returns(employment);

        var result = await _handler.Handle(
            new SeparateEmploymentCommand(employment.Id.Value, tenantId, SeparationType.Retired, null, date, date),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employment.LifecycleStage.Should().Be(EmploymentLifecycleStage.Separated);
    }

    [Fact]
    public async Task Handle_Fails_WhenEmploymentNotFound()
    {
        var tenantId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime);
        _repository.GetByIdAsync(Arg.Any<EmploymentId>(), Arg.Any<CancellationToken>())
            .Returns((Hris.Modules.Employment.Domain.Employment?)null);

        var result = await _handler.Handle(
            new SeparateEmploymentCommand(Guid.NewGuid(), tenantId, SeparationType.Resigned, null, date, date),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentNotFound);
    }
}

public sealed class StartProbationCommandHandlerTests
{
    private readonly IEmploymentRepository _repository = Substitute.For<IEmploymentRepository>();
    private readonly StartProbationCommandHandler _handler;

    public StartProbationCommandHandlerTests()
    {
        _handler = new StartProbationCommandHandler(_repository, new FakeTimeProvider(TestEmployment.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenEmploymentIsActive()
    {
        var tenantId = Guid.NewGuid();
        var employment = TestEmployment.CreateActive(tenantId);
        _repository.GetByIdAsync(employment.Id, Arg.Any<CancellationToken>()).Returns(employment);

        var result = await _handler.Handle(
            new StartProbationCommand(employment.Id.Value, tenantId, DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime), 180),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employment.ProbationRecords.Should().HaveCount(1);
    }
}

public sealed class ExtendProbationCommandHandlerTests
{
    private readonly IEmploymentRepository _repository = Substitute.For<IEmploymentRepository>();
    private readonly ExtendProbationCommandHandler _handler;

    public ExtendProbationCommandHandlerTests()
    {
        _handler = new ExtendProbationCommandHandler(_repository, new FakeTimeProvider(TestEmployment.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenProbationInProgress()
    {
        var tenantId = Guid.NewGuid();
        var employment = TestEmployment.CreateActive(tenantId);
        employment.StartProbation(DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime), 180, TestEmployment.NowUtc);
        _repository.GetByIdAsync(employment.Id, Arg.Any<CancellationToken>()).Returns(employment);

        var result = await _handler.Handle(new ExtendProbationCommand(employment.Id.Value, tenantId, 30), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employment.ProbationRecords[0].ExtensionCount.Should().Be(1);
    }
}

public sealed class ConfirmEmploymentCommandHandlerTests
{
    private readonly IEmploymentRepository _repository = Substitute.For<IEmploymentRepository>();
    private readonly ConfirmEmploymentCommandHandler _handler;

    public ConfirmEmploymentCommandHandlerTests()
    {
        _handler = new ConfirmEmploymentCommandHandler(_repository, new FakeTimeProvider(TestEmployment.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenProbationInProgress()
    {
        var tenantId = Guid.NewGuid();
        var employment = TestEmployment.CreateActive(tenantId);
        employment.StartProbation(DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime), 180, TestEmployment.NowUtc);
        _repository.GetByIdAsync(employment.Id, Arg.Any<CancellationToken>()).Returns(employment);

        var result = await _handler.Handle(new ConfirmEmploymentCommand(employment.Id.Value, tenantId, "Regular"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employment.EmploymentType.Value.Should().Be("Regular");
    }
}

public sealed class FailProbationCommandHandlerTests
{
    private readonly IEmploymentRepository _repository = Substitute.For<IEmploymentRepository>();
    private readonly FailProbationCommandHandler _handler;

    public FailProbationCommandHandlerTests()
    {
        _handler = new FailProbationCommandHandler(_repository, new FakeTimeProvider(TestEmployment.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_SeparatingEmployment()
    {
        var tenantId = Guid.NewGuid();
        var employment = TestEmployment.CreateActive(tenantId);
        var date = DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime);
        employment.StartProbation(date, 180, TestEmployment.NowUtc);
        _repository.GetByIdAsync(employment.Id, Arg.Any<CancellationToken>()).Returns(employment);

        var result = await _handler.Handle(new FailProbationCommand(employment.Id.Value, tenantId, date, date), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employment.LifecycleStage.Should().Be(EmploymentLifecycleStage.Separated);
    }
}

public sealed class RecordEmploymentCompensationCommandHandlerTests
{
    private readonly IEmploymentRepository _repository = Substitute.For<IEmploymentRepository>();
    private readonly RecordEmploymentCompensationCommandHandler _handler;

    public RecordEmploymentCompensationCommandHandlerTests()
    {
        _handler = new RecordEmploymentCompensationCommandHandler(_repository, new FakeTimeProvider(TestEmployment.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenEmploymentExists()
    {
        var tenantId = Guid.NewGuid();
        var employment = TestEmployment.CreateActive(tenantId);
        _repository.GetByIdAsync(employment.Id, Arg.Any<CancellationToken>()).Returns(employment);

        var result = await _handler.Handle(
            new RecordEmploymentCompensationCommand(
                employment.Id.Value, tenantId, 50000m, "PHP", CompensationBasis.Monthly,
                DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime), CompensationChangeSource.Hire, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employment.CompensationRecords.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_Fails_WhenAmountIsNegative()
    {
        var tenantId = Guid.NewGuid();
        var employment = TestEmployment.CreateActive(tenantId);
        _repository.GetByIdAsync(employment.Id, Arg.Any<CancellationToken>()).Returns(employment);

        var result = await _handler.Handle(
            new RecordEmploymentCompensationCommand(
                employment.Id.Value, tenantId, -1m, "PHP", CompensationBasis.Monthly,
                DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime), CompensationChangeSource.Hire, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.CompensationAmountNegative);
    }
}
