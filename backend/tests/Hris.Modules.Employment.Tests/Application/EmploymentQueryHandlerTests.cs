using FluentAssertions;
using Hris.Modules.Employment.Application.Queries;
using Hris.Modules.Employment.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Employment.Tests.Application;

public sealed class GetEmploymentQueryHandlerTests
{
    private readonly IEmploymentRepository _repository = Substitute.For<IEmploymentRepository>();
    private readonly GetEmploymentQueryHandler _handler;

    public GetEmploymentQueryHandlerTests()
    {
        _handler = new GetEmploymentQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsDto_WhenEmploymentExists()
    {
        var tenantId = Guid.NewGuid();
        var employment = TestEmployment.Create(tenantId);
        _repository.GetByIdAsync(employment.Id, Arg.Any<CancellationToken>()).Returns(employment);

        var result = await _handler.Handle(new GetEmploymentQuery(employment.Id.Value, tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Number.Should().Be("EMP-000001");
    }

    [Fact]
    public async Task Handle_Fails_WhenTenantDoesNotMatch()
    {
        var employment = TestEmployment.Create(Guid.NewGuid());
        _repository.GetByIdAsync(employment.Id, Arg.Any<CancellationToken>()).Returns(employment);

        var result = await _handler.Handle(new GetEmploymentQuery(employment.Id.Value, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentNotFound);
    }
}

public sealed class ListEmploymentsQueryHandlerTests
{
    private readonly IEmploymentRepository _repository = Substitute.For<IEmploymentRepository>();
    private readonly ListEmploymentsQueryHandler _handler;

    public ListEmploymentsQueryHandlerTests()
    {
        _handler = new ListEmploymentsQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsAllEmployments_WhenNoFiltersApplied()
    {
        var tenantId = Guid.NewGuid();
        var employments = new List<Hris.Modules.Employment.Domain.Employment> { TestEmployment.Create(tenantId) };
        _repository.ListByTenantAsync(tenantId, Arg.Any<CancellationToken>()).Returns(employments);

        var result = await _handler.Handle(new ListEmploymentsQuery(tenantId, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_FiltersByLifecycleStage()
    {
        var tenantId = Guid.NewGuid();
        var draft = TestEmployment.Create(tenantId);
        var active = TestEmployment.CreateActive(tenantId);
        _repository.ListByTenantAsync(tenantId, Arg.Any<CancellationToken>()).Returns([draft, active]);

        var result = await _handler.Handle(
            new ListEmploymentsQuery(tenantId, null, EmploymentLifecycleStage.Active), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].LifecycleStage.Should().Be(nameof(EmploymentLifecycleStage.Active));
    }

    [Fact]
    public async Task Handle_ListsByEmployeeId_WhenSupplied()
    {
        var tenantId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var employments = new List<Hris.Modules.Employment.Domain.Employment> { TestEmployment.Create(tenantId, employeeId) };
        _repository.ListByEmployeeIdAsync(tenantId, employeeId, Arg.Any<CancellationToken>()).Returns(employments);

        var result = await _handler.Handle(new ListEmploymentsQuery(tenantId, employeeId, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }
}

public sealed class GetEmploymentContractQueryHandlerTests
{
    private readonly IEmploymentContractRepository _repository = Substitute.For<IEmploymentContractRepository>();
    private readonly GetEmploymentContractQueryHandler _handler;

    public GetEmploymentContractQueryHandlerTests()
    {
        _handler = new GetEmploymentContractQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsDto_WhenContractExists()
    {
        var tenantId = Guid.NewGuid();
        var contract = TestEmployment.CreateContract(tenantId, Guid.NewGuid());
        _repository.GetByIdAsync(contract.Id, Arg.Any<CancellationToken>()).Returns(contract);

        var result = await _handler.Handle(new GetEmploymentContractQuery(contract.Id.Value, tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}

public sealed class ListEmploymentContractsQueryHandlerTests
{
    private readonly IEmploymentContractRepository _repository = Substitute.For<IEmploymentContractRepository>();
    private readonly ListEmploymentContractsQueryHandler _handler;

    public ListEmploymentContractsQueryHandlerTests()
    {
        _handler = new ListEmploymentContractsQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsContracts_ForEmployment()
    {
        var tenantId = Guid.NewGuid();
        var employmentId = Guid.NewGuid();
        var contracts = new List<EmploymentContract> { TestEmployment.CreateContract(tenantId, employmentId) };
        _repository.ListByEmploymentIdAsync(tenantId, employmentId, Arg.Any<CancellationToken>()).Returns(contracts);

        var result = await _handler.Handle(new ListEmploymentContractsQuery(tenantId, employmentId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }
}

public sealed class GetEmploymentAssignmentQueryHandlerTests
{
    private readonly IEmploymentAssignmentRepository _repository = Substitute.For<IEmploymentAssignmentRepository>();
    private readonly GetEmploymentAssignmentQueryHandler _handler;

    public GetEmploymentAssignmentQueryHandlerTests()
    {
        _handler = new GetEmploymentAssignmentQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsDto_WhenAssignmentExists()
    {
        var tenantId = Guid.NewGuid();
        var assignment = TestEmployment.CreateAssignment(tenantId, Guid.NewGuid());
        _repository.GetByIdAsync(assignment.Id, Arg.Any<CancellationToken>()).Returns(assignment);

        var result = await _handler.Handle(new GetEmploymentAssignmentQuery(assignment.Id.Value, tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}

public sealed class GetCurrentEmploymentAssignmentQueryHandlerTests
{
    private readonly IEmploymentAssignmentRepository _repository = Substitute.For<IEmploymentAssignmentRepository>();
    private readonly GetCurrentEmploymentAssignmentQueryHandler _handler;

    public GetCurrentEmploymentAssignmentQueryHandlerTests()
    {
        _handler = new GetCurrentEmploymentAssignmentQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsDto_WhenCurrentAssignmentExists()
    {
        var tenantId = Guid.NewGuid();
        var employmentId = Guid.NewGuid();
        var assignment = TestEmployment.CreateAssignment(tenantId, employmentId);
        _repository.GetCurrentByEmploymentIdAsync(tenantId, employmentId, Arg.Any<CancellationToken>()).Returns(assignment);

        var result = await _handler.Handle(new GetCurrentEmploymentAssignmentQuery(tenantId, employmentId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Fails_WhenNoCurrentAssignmentExists()
    {
        var tenantId = Guid.NewGuid();
        var employmentId = Guid.NewGuid();
        _repository.GetCurrentByEmploymentIdAsync(tenantId, employmentId, Arg.Any<CancellationToken>())
            .Returns((EmploymentAssignment?)null);

        var result = await _handler.Handle(new GetCurrentEmploymentAssignmentQuery(tenantId, employmentId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentAssignmentNotFound);
    }
}
