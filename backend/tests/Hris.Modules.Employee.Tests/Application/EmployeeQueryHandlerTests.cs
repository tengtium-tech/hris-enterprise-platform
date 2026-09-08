using FluentAssertions;
using Hris.Modules.Employee.Application.Queries;
using Hris.Modules.Employee.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Employee.Tests.Application;

public sealed class GetEmployeeQueryHandlerTests
{
    private readonly IEmployeeRepository _repository = Substitute.For<IEmployeeRepository>();
    private readonly GetEmployeeQueryHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetEmployeeQueryHandlerTests()
    {
        _handler = new GetEmployeeQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsDto_WhenEmployeeExists()
    {
        var employee = TestEmployee.CreateActive(_tenantId);
        _repository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        var result = await _handler.Handle(new GetEmployeeQuery(employee.Id.Value, _tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Number.Should().Be(employee.Number.Value);
    }

    [Fact]
    public async Task Handle_Fails_WhenEmployeeNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<EmployeeId>(), Arg.Any<CancellationToken>()).Returns((Hris.Modules.Employee.Domain.Employee?)null);

        var result = await _handler.Handle(new GetEmployeeQuery(Guid.NewGuid(), _tenantId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.EmployeeNotFound);
    }

    [Fact]
    public async Task Handle_Fails_WhenEmployeeBelongsToDifferentTenant()
    {
        var employee = TestEmployee.CreateActive(Guid.NewGuid());
        _repository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        var result = await _handler.Handle(new GetEmployeeQuery(employee.Id.Value, _tenantId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.EmployeeNotFound);
    }
}

public sealed class ListEmployeesQueryHandlerTests
{
    private readonly IEmployeeRepository _repository = Substitute.For<IEmployeeRepository>();
    private readonly ListEmployeesQueryHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public ListEmployeesQueryHandlerTests()
    {
        _handler = new ListEmployeesQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsAllEmployees_WhenNoFilterApplied()
    {
        var employee = TestEmployee.CreateActive(_tenantId);
        _repository.ListByTenantAsync(_tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<Hris.Modules.Employee.Domain.Employee> { employee });

        var result = await _handler.Handle(new ListEmployeesQuery(_tenantId, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_FiltersLifecycleStage_WhenFilterApplied()
    {
        var active = TestEmployee.CreateActive(_tenantId);
        var hired = TestEmployee.Create(_tenantId);
        _repository.ListByTenantAsync(_tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<Hris.Modules.Employee.Domain.Employee> { active, hired });

        var result = await _handler.Handle(new ListEmployeesQuery(_tenantId, "Active"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].Id.Should().Be(active.Id.Value);
    }
}

public sealed class GetEmployeeHistoryQueryHandlerTests
{
    private readonly IEmployeeRepository _employeeRepository = Substitute.For<IEmployeeRepository>();
    private readonly IEmployeeHistoryRepository _historyRepository = Substitute.For<IEmployeeHistoryRepository>();
    private readonly GetEmployeeHistoryQueryHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetEmployeeHistoryQueryHandlerTests()
    {
        _handler = new GetEmployeeHistoryQueryHandler(_employeeRepository, _historyRepository);
    }

    [Fact]
    public async Task Handle_ReturnsHistory_WhenEmployeeExists()
    {
        var employee = TestEmployee.CreateActive(_tenantId);
        _employeeRepository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        var history = EmployeeHistory.Record(
            new EmployeeHistoryId(Guid.NewGuid()), _tenantId, employee.Id.Value, EmployeeHistoryCategory.LifecycleStage,
            "Hired", "Active", DateOnly.FromDateTime(TestEmployee.NowUtc.UtcDateTime), null, null, TestEmployee.NowUtc);
        _historyRepository.ListByEmployeeIdAsync(_tenantId, employee.Id.Value, Arg.Any<CancellationToken>())
            .Returns(new List<EmployeeHistory> { history });

        var result = await _handler.Handle(new GetEmployeeHistoryQuery(employee.Id.Value, _tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_Fails_WhenEmployeeNotFound()
    {
        _employeeRepository.GetByIdAsync(Arg.Any<EmployeeId>(), Arg.Any<CancellationToken>()).Returns((Hris.Modules.Employee.Domain.Employee?)null);

        var result = await _handler.Handle(new GetEmployeeHistoryQuery(Guid.NewGuid(), _tenantId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.EmployeeNotFound);
    }
}
