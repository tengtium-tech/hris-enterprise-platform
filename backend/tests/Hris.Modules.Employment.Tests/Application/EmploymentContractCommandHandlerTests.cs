using FluentAssertions;
using Hris.Modules.Employment.Application.Commands;
using Hris.Modules.Employment.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Employment.Tests.Application;

public sealed class CreateEmploymentContractCommandHandlerTests
{
    private readonly IEmploymentContractRepository _repository = Substitute.For<IEmploymentContractRepository>();
    private readonly CreateEmploymentContractCommandHandler _handler;

    public CreateEmploymentContractCommandHandlerTests()
    {
        _handler = new CreateEmploymentContractCommandHandler(_repository, new FakeTimeProvider(TestEmployment.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WithIndefiniteContract()
    {
        var result = await _handler.Handle(
            new CreateEmploymentContractCommand(
                Guid.NewGuid(), Guid.NewGuid(), "Regular", DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime), null,
                false, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<EmploymentContract>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenFixedTermWithoutEndDate()
    {
        var result = await _handler.Handle(
            new CreateEmploymentContractCommand(
                Guid.NewGuid(), Guid.NewGuid(), "Contractual", DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime), null,
                true, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.FixedTermContractRequiresEndDate);
    }
}

public sealed class ApproveEmploymentContractCommandHandlerTests
{
    private readonly IEmploymentContractRepository _repository = Substitute.For<IEmploymentContractRepository>();
    private readonly ApproveEmploymentContractCommandHandler _handler;

    public ApproveEmploymentContractCommandHandlerTests()
    {
        _handler = new ApproveEmploymentContractCommandHandler(_repository, new FakeTimeProvider(TestEmployment.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenContractIsDraft()
    {
        var tenantId = Guid.NewGuid();
        var contract = TestEmployment.CreateContract(tenantId, Guid.NewGuid());
        _repository.GetByIdAsync(contract.Id, Arg.Any<CancellationToken>()).Returns(contract);

        var result = await _handler.Handle(new ApproveEmploymentContractCommand(contract.Id.Value, tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        contract.LifecycleStage.Should().Be(ContractLifecycleStage.Approved);
    }

    [Fact]
    public async Task Handle_Fails_WhenContractNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<EmploymentContractId>(), Arg.Any<CancellationToken>())
            .Returns((EmploymentContract?)null);

        var result = await _handler.Handle(
            new ApproveEmploymentContractCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentContractNotFound);
    }
}

public sealed class ActivateEmploymentContractCommandHandlerTests
{
    private readonly IEmploymentContractRepository _repository = Substitute.For<IEmploymentContractRepository>();
    private readonly ActivateEmploymentContractCommandHandler _handler;

    public ActivateEmploymentContractCommandHandlerTests()
    {
        _handler = new ActivateEmploymentContractCommandHandler(_repository, new FakeTimeProvider(TestEmployment.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenContractIsApproved()
    {
        var tenantId = Guid.NewGuid();
        var contract = TestEmployment.CreateContract(tenantId, Guid.NewGuid());
        contract.Approve(TestEmployment.NowUtc);
        _repository.GetByIdAsync(contract.Id, Arg.Any<CancellationToken>()).Returns(contract);

        var result = await _handler.Handle(new ActivateEmploymentContractCommand(contract.Id.Value, tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        contract.LifecycleStage.Should().Be(ContractLifecycleStage.Effective);
    }
}

public sealed class RenewEmploymentContractCommandHandlerTests
{
    private readonly IEmploymentContractRepository _repository = Substitute.For<IEmploymentContractRepository>();
    private readonly RenewEmploymentContractCommandHandler _handler;

    public RenewEmploymentContractCommandHandlerTests()
    {
        _handler = new RenewEmploymentContractCommandHandler(_repository, new FakeTimeProvider(TestEmployment.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenContractIsEffective()
    {
        var tenantId = Guid.NewGuid();
        var contract = TestEmployment.CreateContract(tenantId, Guid.NewGuid());
        contract.Approve(TestEmployment.NowUtc);
        contract.MakeEffective(TestEmployment.NowUtc);
        _repository.GetByIdAsync(contract.Id, Arg.Any<CancellationToken>()).Returns(contract);
        var newStart = DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime).AddYears(1);

        var result = await _handler.Handle(
            new RenewEmploymentContractCommand(contract.Id.Value, tenantId, newStart, null, "Approved"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        contract.Renewals.Should().HaveCount(1);
    }
}

public sealed class ExtendEmploymentContractCommandHandlerTests
{
    private readonly IEmploymentContractRepository _repository = Substitute.For<IEmploymentContractRepository>();
    private readonly ExtendEmploymentContractCommandHandler _handler;

    public ExtendEmploymentContractCommandHandlerTests()
    {
        _handler = new ExtendEmploymentContractCommandHandler(_repository, new FakeTimeProvider(TestEmployment.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenContractIsEffective()
    {
        var tenantId = Guid.NewGuid();
        var start = DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime);
        var contract = EmploymentContract.Create(
            new EmploymentContractId(Guid.NewGuid()), tenantId, Guid.NewGuid(), "Contractual", start,
            start.AddMonths(6), true, null, TestEmployment.NowUtc).Value;
        contract.Approve(TestEmployment.NowUtc);
        contract.MakeEffective(TestEmployment.NowUtc);
        _repository.GetByIdAsync(contract.Id, Arg.Any<CancellationToken>()).Returns(contract);

        var result = await _handler.Handle(
            new ExtendEmploymentContractCommand(contract.Id.Value, tenantId, start.AddMonths(9), "Extended"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        contract.Extensions.Should().HaveCount(1);
    }
}

public sealed class SupersedeEmploymentContractCommandHandlerTests
{
    private readonly IEmploymentContractRepository _repository = Substitute.For<IEmploymentContractRepository>();
    private readonly SupersedeEmploymentContractCommandHandler _handler;

    public SupersedeEmploymentContractCommandHandlerTests()
    {
        _handler = new SupersedeEmploymentContractCommandHandler(_repository, new FakeTimeProvider(TestEmployment.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenContractIsEffective()
    {
        var tenantId = Guid.NewGuid();
        var contract = TestEmployment.CreateContract(tenantId, Guid.NewGuid());
        contract.Approve(TestEmployment.NowUtc);
        contract.MakeEffective(TestEmployment.NowUtc);
        _repository.GetByIdAsync(contract.Id, Arg.Any<CancellationToken>()).Returns(contract);

        var result = await _handler.Handle(new SupersedeEmploymentContractCommand(contract.Id.Value, tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        contract.LifecycleStage.Should().Be(ContractLifecycleStage.Superseded);
    }
}

public sealed class CloseEmploymentContractCommandHandlerTests
{
    private readonly IEmploymentContractRepository _repository = Substitute.For<IEmploymentContractRepository>();
    private readonly CloseEmploymentContractCommandHandler _handler;

    public CloseEmploymentContractCommandHandlerTests()
    {
        _handler = new CloseEmploymentContractCommandHandler(_repository, new FakeTimeProvider(TestEmployment.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenContractIsEffective()
    {
        var tenantId = Guid.NewGuid();
        var contract = TestEmployment.CreateContract(tenantId, Guid.NewGuid());
        contract.Approve(TestEmployment.NowUtc);
        contract.MakeEffective(TestEmployment.NowUtc);
        _repository.GetByIdAsync(contract.Id, Arg.Any<CancellationToken>()).Returns(contract);

        var result = await _handler.Handle(new CloseEmploymentContractCommand(contract.Id.Value, tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        contract.LifecycleStage.Should().Be(ContractLifecycleStage.Closed);
    }
}

public sealed class CancelEmploymentContractCommandHandlerTests
{
    private readonly IEmploymentContractRepository _repository = Substitute.For<IEmploymentContractRepository>();
    private readonly CancelEmploymentContractCommandHandler _handler;

    public CancelEmploymentContractCommandHandlerTests()
    {
        _handler = new CancelEmploymentContractCommandHandler(_repository, new FakeTimeProvider(TestEmployment.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenContractIsDraft()
    {
        var tenantId = Guid.NewGuid();
        var contract = TestEmployment.CreateContract(tenantId, Guid.NewGuid());
        _repository.GetByIdAsync(contract.Id, Arg.Any<CancellationToken>()).Returns(contract);

        var result = await _handler.Handle(new CancelEmploymentContractCommand(contract.Id.Value, tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        contract.LifecycleStage.Should().Be(ContractLifecycleStage.Cancelled);
    }
}
