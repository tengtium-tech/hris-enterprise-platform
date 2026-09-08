using FluentAssertions;
using Hris.Modules.Employee.Application.Commands;
using Hris.Modules.Employee.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Employee.Tests.Application;

public sealed class AddEmergencyContactCommandHandlerTests
{
    private readonly IEmployeeRepository _repository = Substitute.For<IEmployeeRepository>();
    private readonly AddEmergencyContactCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public AddEmergencyContactCommandHandlerTests()
    {
        _handler = new AddEmergencyContactCommandHandler(_repository, new FakeTimeProvider(TestEmployee.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenEmployeeExists()
    {
        var employee = TestEmployee.CreateActive(_tenantId);
        _repository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        var result = await _handler.Handle(
            new AddEmergencyContactCommand(employee.Id.Value, _tenantId, "Maria Santos", "Spouse", "+63 917 111 2222", null, true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        employee.EmergencyContacts.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_Fails_WhenEmployeeNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<EmployeeId>(), Arg.Any<CancellationToken>()).Returns((Hris.Modules.Employee.Domain.Employee?)null);

        var result = await _handler.Handle(
            new AddEmergencyContactCommand(Guid.NewGuid(), _tenantId, "Maria Santos", "Spouse", "+63 917 111 2222", null, true),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.EmployeeNotFound);
    }
}

public sealed class UpdateEmergencyContactCommandHandlerTests
{
    private readonly IEmployeeRepository _repository = Substitute.For<IEmployeeRepository>();
    private readonly UpdateEmergencyContactCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public UpdateEmergencyContactCommandHandlerTests()
    {
        _handler = new UpdateEmergencyContactCommandHandler(_repository, new FakeTimeProvider(TestEmployee.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenContactExists()
    {
        var employee = TestEmployee.CreateActive(_tenantId);
        employee.AddEmergencyContact("Maria Santos", "Spouse", "+63 917 111 2222", null, true, TestEmployee.NowUtc);
        var contactId = employee.EmergencyContacts[0].Id.Value;
        _repository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        var result = await _handler.Handle(
            new UpdateEmergencyContactCommand(employee.Id.Value, _tenantId, contactId, "Maria Reyes", "Spouse", "+63 917 999 8888", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employee.EmergencyContacts[0].Name.Should().Be("Maria Reyes");
    }
}

public sealed class RemoveEmergencyContactCommandHandlerTests
{
    private readonly IEmployeeRepository _repository = Substitute.For<IEmployeeRepository>();
    private readonly RemoveEmergencyContactCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public RemoveEmergencyContactCommandHandlerTests()
    {
        _handler = new RemoveEmergencyContactCommandHandler(_repository, new FakeTimeProvider(TestEmployee.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenContactExists()
    {
        var employee = TestEmployee.CreateActive(_tenantId);
        employee.AddEmergencyContact("Maria Santos", "Spouse", "+63 917 111 2222", null, true, TestEmployee.NowUtc);
        var contactId = employee.EmergencyContacts[0].Id.Value;
        _repository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        var result = await _handler.Handle(
            new RemoveEmergencyContactCommand(employee.Id.Value, _tenantId, contactId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employee.EmergencyContacts.Should().BeEmpty();
    }
}
