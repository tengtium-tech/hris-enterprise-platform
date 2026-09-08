using FluentAssertions;
using Hris.Modules.Employee.Application.Commands;
using Hris.Modules.Employee.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Employee.Tests.Application;

public sealed class UpdateEmployeeGovernmentInformationCommandHandlerTests
{
    private readonly IEmployeeRepository _repository = Substitute.For<IEmployeeRepository>();
    private readonly IEmployeeHistoryRepository _historyRepository = Substitute.For<IEmployeeHistoryRepository>();
    private readonly UpdateEmployeeGovernmentInformationCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public UpdateEmployeeGovernmentInformationCommandHandlerTests()
    {
        _handler = new UpdateEmployeeGovernmentInformationCommandHandler(
            _repository, _historyRepository, new FakeTimeProvider(TestEmployee.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenEmployeeExists()
    {
        var employee = TestEmployee.CreateActive(_tenantId);
        _repository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        var result = await _handler.Handle(
            new UpdateEmployeeGovernmentInformationCommand(employee.Id.Value, _tenantId, "123-456-789", null, null, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employee.Tin!.Value.Should().Be("123-456-789");
    }

    [Fact]
    public async Task Handle_Fails_WhenEmployeeNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<EmployeeId>(), Arg.Any<CancellationToken>()).Returns((Hris.Modules.Employee.Domain.Employee?)null);

        var result = await _handler.Handle(
            new UpdateEmployeeGovernmentInformationCommand(Guid.NewGuid(), _tenantId, null, null, null, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.EmployeeNotFound);
    }
}

public sealed class UpdateEmployeeBankInformationCommandHandlerTests
{
    private readonly IEmployeeRepository _repository = Substitute.For<IEmployeeRepository>();
    private readonly IEmployeeHistoryRepository _historyRepository = Substitute.For<IEmployeeHistoryRepository>();
    private readonly UpdateEmployeeBankInformationCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public UpdateEmployeeBankInformationCommandHandlerTests()
    {
        _handler = new UpdateEmployeeBankInformationCommandHandler(
            _repository, _historyRepository, new FakeTimeProvider(TestEmployee.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenEmployeeExists()
    {
        var employee = TestEmployee.CreateActive(_tenantId);
        _repository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        var result = await _handler.Handle(
            new UpdateEmployeeBankInformationCommand(employee.Id.Value, _tenantId, "BDO", null, null, "1234567890", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employee.Banking!.BankName.Should().Be("BDO");
    }

    [Fact]
    public async Task Handle_Fails_WhenEmployeeNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<EmployeeId>(), Arg.Any<CancellationToken>()).Returns((Hris.Modules.Employee.Domain.Employee?)null);

        var result = await _handler.Handle(
            new UpdateEmployeeBankInformationCommand(Guid.NewGuid(), _tenantId, null, null, null, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.EmployeeNotFound);
    }
}
