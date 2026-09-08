using FluentAssertions;
using Hris.Modules.Employee.Application.Commands;
using Hris.Modules.Employee.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Employee.Tests.Application;

public sealed class UpdateEmployeeContactInformationCommandHandlerTests
{
    private readonly IEmployeeRepository _repository = Substitute.For<IEmployeeRepository>();
    private readonly IEmployeeHistoryRepository _historyRepository = Substitute.For<IEmployeeHistoryRepository>();
    private readonly UpdateEmployeeContactInformationCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public UpdateEmployeeContactInformationCommandHandlerTests()
    {
        _handler = new UpdateEmployeeContactInformationCommandHandler(
            _repository, _historyRepository, new FakeTimeProvider(TestEmployee.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenEmployeeExists()
    {
        var employee = TestEmployee.CreateActive(_tenantId);
        _repository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        var result = await _handler.Handle(
            new UpdateEmployeeContactInformationCommand(
                employee.Id.Value, _tenantId, "juan@example.com", null, "+63 917 123 4567", null, "123 Main St", null,
                "Manila", null, null, "Philippines", null, null, null, null, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employee.PersonalEmail!.Value.Should().Be("juan@example.com");
        await _historyRepository.Received(1).AddAsync(Arg.Any<EmployeeHistory>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenEmployeeNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<EmployeeId>(), Arg.Any<CancellationToken>()).Returns((Hris.Modules.Employee.Domain.Employee?)null);

        var result = await _handler.Handle(
            new UpdateEmployeeContactInformationCommand(
                Guid.NewGuid(), _tenantId, null, null, null, null, null, null, null, null, null, null, null, null, null,
                null, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.EmployeeNotFound);
    }
}
