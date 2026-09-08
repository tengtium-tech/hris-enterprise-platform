using FluentAssertions;
using Hris.Modules.Employee.Application.Commands;
using Hris.Modules.Employee.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Employee.Tests.Application;

public sealed class EmployeeLifecycleCommandHandlerTests
{
    private readonly IEmployeeRepository _repository = Substitute.For<IEmployeeRepository>();
    private readonly IEmployeeHistoryRepository _historyRepository = Substitute.For<IEmployeeHistoryRepository>();
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public async Task StartEmployeeOnboarding_Succeeds_WhenHired()
    {
        var employee = TestEmployee.Create(_tenantId);
        _repository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        var handler = new StartEmployeeOnboardingCommandHandler(_repository, _historyRepository, new FakeTimeProvider(TestEmployee.NowUtc));

        var result = await handler.Handle(new StartEmployeeOnboardingCommand(employee.Id.Value, _tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employee.LifecycleStage.Should().Be(EmployeeLifecycleStage.Onboarding);
        await _historyRepository.Received(1).AddAsync(Arg.Any<EmployeeHistory>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartEmployeeOnboarding_Fails_WhenEmployeeNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<EmployeeId>(), Arg.Any<CancellationToken>()).Returns((Hris.Modules.Employee.Domain.Employee?)null);
        var handler = new StartEmployeeOnboardingCommandHandler(_repository, _historyRepository, new FakeTimeProvider(TestEmployee.NowUtc));

        var result = await handler.Handle(new StartEmployeeOnboardingCommand(Guid.NewGuid(), _tenantId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.EmployeeNotFound);
    }

    [Fact]
    public async Task ActivateEmployee_Succeeds_WhenOnboarding()
    {
        var employee = TestEmployee.Create(_tenantId);
        employee.StartOnboarding(TestEmployee.NowUtc);
        _repository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        var handler = new ActivateEmployeeCommandHandler(_repository, _historyRepository, new FakeTimeProvider(TestEmployee.NowUtc));

        var result = await handler.Handle(new ActivateEmployeeCommand(employee.Id.Value, _tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employee.LifecycleStage.Should().Be(EmployeeLifecycleStage.Active);
    }

    [Fact]
    public async Task ActivateEmployee_Fails_WhenNotOnboarding()
    {
        var employee = TestEmployee.Create(_tenantId);
        _repository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        var handler = new ActivateEmployeeCommandHandler(_repository, _historyRepository, new FakeTimeProvider(TestEmployee.NowUtc));

        var result = await handler.Handle(new ActivateEmployeeCommand(employee.Id.Value, _tenantId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.EmployeeNotOnboarding);
    }

    [Fact]
    public async Task StartEmployeeOffboarding_Succeeds_WhenActive()
    {
        var employee = TestEmployee.CreateActive(_tenantId);
        _repository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        var handler = new StartEmployeeOffboardingCommandHandler(_repository, _historyRepository, new FakeTimeProvider(TestEmployee.NowUtc));

        var result = await handler.Handle(new StartEmployeeOffboardingCommand(employee.Id.Value, _tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employee.LifecycleStage.Should().Be(EmployeeLifecycleStage.Offboarding);
    }

    [Fact]
    public async Task SeparateEmployee_Succeeds_WhenOffboarding()
    {
        var employee = TestEmployee.CreateActive(_tenantId);
        employee.StartOffboarding(TestEmployee.NowUtc);
        _repository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        var handler = new SeparateEmployeeCommandHandler(_repository, _historyRepository, new FakeTimeProvider(TestEmployee.NowUtc));

        var result = await handler.Handle(new SeparateEmployeeCommand(employee.Id.Value, _tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employee.LifecycleStage.Should().Be(EmployeeLifecycleStage.Separated);
    }

    [Fact]
    public async Task RetireEmployee_Succeeds_WhenSeparated()
    {
        var employee = TestEmployee.CreateSeparated(_tenantId);
        _repository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        var handler = new RetireEmployeeCommandHandler(_repository, _historyRepository, new FakeTimeProvider(TestEmployee.NowUtc));

        var result = await handler.Handle(new RetireEmployeeCommand(employee.Id.Value, _tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employee.LifecycleStage.Should().Be(EmployeeLifecycleStage.Retired);
    }

    [Fact]
    public async Task RehireEmployee_Succeeds_WhenSeparated()
    {
        var employee = TestEmployee.CreateSeparated(_tenantId);
        _repository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        var handler = new RehireEmployeeCommandHandler(_repository, _historyRepository, new FakeTimeProvider(TestEmployee.NowUtc));

        var result = await handler.Handle(new RehireEmployeeCommand(employee.Id.Value, _tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employee.LifecycleStage.Should().Be(EmployeeLifecycleStage.Active);
    }

    [Fact]
    public async Task RecordEmployeeDeceased_Succeeds_WhenActive()
    {
        var employee = TestEmployee.CreateActive(_tenantId);
        _repository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        var handler = new RecordEmployeeDeceasedCommandHandler(_repository, _historyRepository, new FakeTimeProvider(TestEmployee.NowUtc));

        var result = await handler.Handle(new RecordEmployeeDeceasedCommand(employee.Id.Value, _tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employee.LifecycleStage.Should().Be(EmployeeLifecycleStage.Deceased);
    }

    [Fact]
    public async Task RecordEmployeeDeceased_Fails_WhenAlreadyDeceased()
    {
        var employee = TestEmployee.CreateActive(_tenantId);
        employee.RecordDeceased(TestEmployee.NowUtc);
        _repository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        var handler = new RecordEmployeeDeceasedCommandHandler(_repository, _historyRepository, new FakeTimeProvider(TestEmployee.NowUtc));

        var result = await handler.Handle(new RecordEmployeeDeceasedCommand(employee.Id.Value, _tenantId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.EmployeeAlreadyDeceased);
    }
}
