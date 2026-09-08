using FluentAssertions;
using Hris.Modules.Employee.Application.Commands;
using Hris.Modules.Employee.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Employee.Tests.Application;

public sealed class AddFamilyMemberCommandHandlerTests
{
    private readonly IEmployeeRepository _repository = Substitute.For<IEmployeeRepository>();
    private readonly AddFamilyMemberCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public AddFamilyMemberCommandHandlerTests()
    {
        _handler = new AddFamilyMemberCommandHandler(_repository, new FakeTimeProvider(TestEmployee.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenEmployeeExists()
    {
        var employee = TestEmployee.CreateActive(_tenantId);
        _repository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        var result = await _handler.Handle(
            new AddFamilyMemberCommand(employee.Id.Value, _tenantId, "Maria Dela Cruz", FamilyRelationship.Spouse, null, false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        employee.FamilyMembers.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_Fails_WhenEmployeeNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<EmployeeId>(), Arg.Any<CancellationToken>()).Returns((Hris.Modules.Employee.Domain.Employee?)null);

        var result = await _handler.Handle(
            new AddFamilyMemberCommand(Guid.NewGuid(), _tenantId, "Maria Dela Cruz", FamilyRelationship.Spouse, null, false),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmployeeErrors.EmployeeNotFound);
    }
}

public sealed class UpdateFamilyMemberCommandHandlerTests
{
    private readonly IEmployeeRepository _repository = Substitute.For<IEmployeeRepository>();
    private readonly UpdateFamilyMemberCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public UpdateFamilyMemberCommandHandlerTests()
    {
        _handler = new UpdateFamilyMemberCommandHandler(_repository, new FakeTimeProvider(TestEmployee.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenMemberExists()
    {
        var employee = TestEmployee.CreateActive(_tenantId);
        employee.AddFamilyMember("Maria Dela Cruz", FamilyRelationship.Spouse, null, false, TestEmployee.NowUtc);
        var memberId = employee.FamilyMembers[0].Id.Value;
        _repository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        var result = await _handler.Handle(
            new UpdateFamilyMemberCommand(employee.Id.Value, _tenantId, memberId, "Maria Reyes", FamilyRelationship.Spouse, null, true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employee.FamilyMembers[0].IsDependent.Should().BeTrue();
    }
}

public sealed class RemoveFamilyMemberCommandHandlerTests
{
    private readonly IEmployeeRepository _repository = Substitute.For<IEmployeeRepository>();
    private readonly RemoveFamilyMemberCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public RemoveFamilyMemberCommandHandlerTests()
    {
        _handler = new RemoveFamilyMemberCommandHandler(_repository, new FakeTimeProvider(TestEmployee.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenMemberExists()
    {
        var employee = TestEmployee.CreateActive(_tenantId);
        employee.AddFamilyMember("Maria Dela Cruz", FamilyRelationship.Spouse, null, false, TestEmployee.NowUtc);
        var memberId = employee.FamilyMembers[0].Id.Value;
        _repository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        var result = await _handler.Handle(
            new RemoveFamilyMemberCommand(employee.Id.Value, _tenantId, memberId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employee.FamilyMembers.Should().BeEmpty();
    }
}
