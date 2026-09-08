using FluentAssertions;
using Hris.Modules.Employee.Application.Commands;
using Hris.Modules.Employee.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Employee.Tests.Application;

public sealed class RegisterEmployeeCommandHandlerTests
{
    private readonly IEmployeeRepository _repository = Substitute.For<IEmployeeRepository>();
    private readonly IEmployeeHistoryRepository _historyRepository = Substitute.For<IEmployeeHistoryRepository>();
    private readonly RegisterEmployeeCommandHandler _handler;

    public RegisterEmployeeCommandHandlerTests()
    {
        _handler = new RegisterEmployeeCommandHandler(_repository, _historyRepository, new FakeTimeProvider(TestEmployee.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenNumberIsUnique()
    {
        var tenantId = Guid.NewGuid();
        _repository.ExistsWithNumberAsync(tenantId, "EMP-000001", null, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(
            new RegisterEmployeeCommand(
                tenantId, "EMP-000001", "Juan", null, "Dela Cruz", null, null, null, new DateOnly(1990, 1, 1), null,
                Gender.Male, CivilStatus.Single, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<Hris.Modules.Employee.Domain.Employee>(), Arg.Any<CancellationToken>());
        await _historyRepository.Received(1).AddAsync(Arg.Any<EmployeeHistory>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenNumberAlreadyExists()
    {
        var tenantId = Guid.NewGuid();
        _repository.ExistsWithNumberAsync(tenantId, "EMP-000001", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(
            new RegisterEmployeeCommand(
                tenantId, "EMP-000001", "Juan", null, "Dela Cruz", null, null, null, new DateOnly(1990, 1, 1), null,
                Gender.Male, CivilStatus.Single, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.DuplicateEmployeeNumber);
    }

    [Fact]
    public async Task Handle_Fails_WhenNameInvalid()
    {
        var tenantId = Guid.NewGuid();
        _repository.ExistsWithNumberAsync(tenantId, "EMP-000001", null, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(
            new RegisterEmployeeCommand(
                tenantId, "EMP-000001", null, null, "Dela Cruz", null, null, null, new DateOnly(1990, 1, 1), null,
                Gender.Male, CivilStatus.Single, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.FirstNameRequired);
    }
}

public sealed class UpdateEmployeePersonalInformationCommandHandlerTests
{
    private readonly IEmployeeRepository _repository = Substitute.For<IEmployeeRepository>();
    private readonly IEmployeeHistoryRepository _historyRepository = Substitute.For<IEmployeeHistoryRepository>();
    private readonly UpdateEmployeePersonalInformationCommandHandler _handler;

    public UpdateEmployeePersonalInformationCommandHandlerTests()
    {
        _handler = new UpdateEmployeePersonalInformationCommandHandler(
            _repository, _historyRepository, new FakeTimeProvider(TestEmployee.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenEmployeeExists()
    {
        var tenantId = Guid.NewGuid();
        var employee = TestEmployee.CreateActive(tenantId);
        _repository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        var result = await _handler.Handle(
            new UpdateEmployeePersonalInformationCommand(
                employee.Id.Value, tenantId, "Juana", null, "Dela Cruz", null, null, null, new DateOnly(1990, 1, 1), null,
                Gender.Female, CivilStatus.Married, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _historyRepository.Received(1).AddAsync(Arg.Any<EmployeeHistory>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenEmployeeNotFound()
    {
        var tenantId = Guid.NewGuid();
        _repository.GetByIdAsync(Arg.Any<EmployeeId>(), Arg.Any<CancellationToken>()).Returns((Hris.Modules.Employee.Domain.Employee?)null);

        var result = await _handler.Handle(
            new UpdateEmployeePersonalInformationCommand(
                Guid.NewGuid(), tenantId, "Juana", null, "Dela Cruz", null, null, null, new DateOnly(1990, 1, 1), null,
                Gender.Female, CivilStatus.Married, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.EmployeeNotFound);
    }
}

public sealed class UpdateEmployeePhotoCommandHandlerTests
{
    private readonly IEmployeeRepository _repository = Substitute.For<IEmployeeRepository>();
    private readonly UpdateEmployeePhotoCommandHandler _handler;

    public UpdateEmployeePhotoCommandHandlerTests()
    {
        _handler = new UpdateEmployeePhotoCommandHandler(_repository, new FakeTimeProvider(TestEmployee.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenEmployeeExists()
    {
        var tenantId = Guid.NewGuid();
        var employee = TestEmployee.CreateActive(tenantId);
        _repository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        var reference = Guid.NewGuid();

        var result = await _handler.Handle(
            new UpdateEmployeePhotoCommand(employee.Id.Value, tenantId, reference), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employee.PhotographReference.Should().Be(reference);
    }
}
