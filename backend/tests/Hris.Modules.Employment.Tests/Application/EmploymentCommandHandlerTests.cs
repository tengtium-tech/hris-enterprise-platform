using FluentAssertions;
using Hris.Modules.Employment.Application.Commands;
using Hris.Modules.Employment.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Employment.Tests.Application;

public sealed class CreateEmploymentCommandHandlerTests
{
    private readonly IEmploymentRepository _repository = Substitute.For<IEmploymentRepository>();
    private readonly CreateEmploymentCommandHandler _handler;

    public CreateEmploymentCommandHandlerTests()
    {
        _handler = new CreateEmploymentCommandHandler(_repository, new FakeTimeProvider(TestEmployment.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenNumberIsUniqueAndNoPrimaryExists()
    {
        var tenantId = Guid.NewGuid();
        _repository.ExistsWithNumberAsync(tenantId, "EMP-000001", null, Arg.Any<CancellationToken>()).Returns(false);
        _repository.GetActivePrimaryEmploymentAsync(tenantId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Hris.Modules.Employment.Domain.Employment?)null);

        var result = await _handler.Handle(
            new CreateEmploymentCommand(tenantId, Guid.NewGuid(), "EMP-000001", "Regular", "Rank-and-File", true, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<Hris.Modules.Employment.Domain.Employment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenNumberAlreadyExists()
    {
        var tenantId = Guid.NewGuid();
        _repository.ExistsWithNumberAsync(tenantId, "EMP-000001", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(
            new CreateEmploymentCommand(tenantId, Guid.NewGuid(), "EMP-000001", "Regular", "Rank-and-File", true, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.DuplicateEmploymentNumber);
    }

    [Fact]
    public async Task Handle_Fails_WhenPrimaryAlreadyExistsAndNotConcurrent()
    {
        var tenantId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        _repository.ExistsWithNumberAsync(tenantId, "EMP-000002", null, Arg.Any<CancellationToken>()).Returns(false);
        _repository.GetActivePrimaryEmploymentAsync(tenantId, employeeId, Arg.Any<CancellationToken>())
            .Returns(TestEmployment.Create(tenantId, employeeId));

        var result = await _handler.Handle(
            new CreateEmploymentCommand(tenantId, employeeId, "EMP-000002", "Regular", "Rank-and-File", true, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.DuplicatePrimaryEmployment);
    }
}

public sealed class ActivateEmploymentCommandHandlerTests
{
    private readonly IEmploymentRepository _employmentRepository = Substitute.For<IEmploymentRepository>();
    private readonly IEmploymentContractRepository _contractRepository = Substitute.For<IEmploymentContractRepository>();
    private readonly IEmploymentAssignmentRepository _assignmentRepository = Substitute.For<IEmploymentAssignmentRepository>();
    private readonly ActivateEmploymentCommandHandler _handler;

    public ActivateEmploymentCommandHandlerTests()
    {
        _handler = new ActivateEmploymentCommandHandler(
            _employmentRepository, _contractRepository, _assignmentRepository, new FakeTimeProvider(TestEmployment.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenContractAndAssignmentAreValid()
    {
        var tenantId = Guid.NewGuid();
        var employment = TestEmployment.Create(tenantId);
        _employmentRepository.GetByIdAsync(employment.Id, Arg.Any<CancellationToken>()).Returns(employment);
        _contractRepository.HasValidContractAsync(tenantId, employment.Id.Value, Arg.Any<CancellationToken>()).Returns(true);
        _assignmentRepository.HasValidAssignmentAsync(tenantId, employment.Id.Value, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(
            new ActivateEmploymentCommand(employment.Id.Value, tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employment.LifecycleStage.Should().Be(EmploymentLifecycleStage.Active);
    }

    [Fact]
    public async Task Handle_Fails_WhenEmploymentNotFound()
    {
        var tenantId = Guid.NewGuid();
        _employmentRepository.GetByIdAsync(Arg.Any<EmploymentId>(), Arg.Any<CancellationToken>())
            .Returns((Hris.Modules.Employment.Domain.Employment?)null);

        var result = await _handler.Handle(new ActivateEmploymentCommand(Guid.NewGuid(), tenantId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentNotFound);
    }

    [Fact]
    public async Task Handle_Fails_WhenNoValidContract()
    {
        var tenantId = Guid.NewGuid();
        var employment = TestEmployment.Create(tenantId);
        _employmentRepository.GetByIdAsync(employment.Id, Arg.Any<CancellationToken>()).Returns(employment);
        _contractRepository.HasValidContractAsync(tenantId, employment.Id.Value, Arg.Any<CancellationToken>()).Returns(false);
        _assignmentRepository.HasValidAssignmentAsync(tenantId, employment.Id.Value, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(
            new ActivateEmploymentCommand(employment.Id.Value, tenantId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentActivationRequiresContract);
    }
}

public sealed class ChangeEmploymentTypeCommandHandlerTests
{
    private readonly IEmploymentRepository _repository = Substitute.For<IEmploymentRepository>();
    private readonly ChangeEmploymentTypeCommandHandler _handler;

    public ChangeEmploymentTypeCommandHandlerTests()
    {
        _handler = new ChangeEmploymentTypeCommandHandler(_repository, new FakeTimeProvider(TestEmployment.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenEmploymentExists()
    {
        var tenantId = Guid.NewGuid();
        var employment = TestEmployment.CreateActive(tenantId);
        _repository.GetByIdAsync(employment.Id, Arg.Any<CancellationToken>()).Returns(employment);

        var result = await _handler.Handle(
            new ChangeEmploymentTypeCommand(employment.Id.Value, tenantId, "Contractual"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employment.EmploymentType.Value.Should().Be("Contractual");
    }

    [Fact]
    public async Task Handle_Fails_WhenEmploymentNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<EmploymentId>(), Arg.Any<CancellationToken>())
            .Returns((Hris.Modules.Employment.Domain.Employment?)null);

        var result = await _handler.Handle(
            new ChangeEmploymentTypeCommand(Guid.NewGuid(), Guid.NewGuid(), "Regular"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentNotFound);
    }
}

public sealed class ChangeEmploymentCategoryCommandHandlerTests
{
    private readonly IEmploymentRepository _repository = Substitute.For<IEmploymentRepository>();
    private readonly ChangeEmploymentCategoryCommandHandler _handler;

    public ChangeEmploymentCategoryCommandHandlerTests()
    {
        _handler = new ChangeEmploymentCategoryCommandHandler(_repository, new FakeTimeProvider(TestEmployment.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenEmploymentExists()
    {
        var tenantId = Guid.NewGuid();
        var employment = TestEmployment.CreateActive(tenantId);
        _repository.GetByIdAsync(employment.Id, Arg.Any<CancellationToken>()).Returns(employment);

        var result = await _handler.Handle(
            new ChangeEmploymentCategoryCommand(employment.Id.Value, tenantId, "Managerial"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employment.Category.Value.Should().Be("Managerial");
    }
}

public sealed class ChangePrimaryEmploymentCommandHandlerTests
{
    private readonly IEmploymentRepository _repository = Substitute.For<IEmploymentRepository>();
    private readonly ChangePrimaryEmploymentCommandHandler _handler;

    public ChangePrimaryEmploymentCommandHandlerTests()
    {
        _handler = new ChangePrimaryEmploymentCommandHandler(_repository, new FakeTimeProvider(TestEmployment.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_MarkingNewPrimaryAndDemotingOld()
    {
        var tenantId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var newPrimary = Hris.Modules.Employment.Domain.Employment.Create(
            new EmploymentId(Guid.NewGuid()), tenantId, employeeId, "EMP-000005", "Consultant", "Rank-and-File", false,
            Guid.NewGuid(), null, true, false, TestEmployment.NowUtc).Value;
        var oldPrimary = TestEmployment.Create(tenantId, employeeId);

        _repository.GetByIdAsync(newPrimary.Id, Arg.Any<CancellationToken>()).Returns(newPrimary);
        _repository.GetActivePrimaryEmploymentAsync(tenantId, employeeId, Arg.Any<CancellationToken>()).Returns(oldPrimary);

        var result = await _handler.Handle(
            new ChangePrimaryEmploymentCommand(newPrimary.Id.Value, tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        newPrimary.IsPrimary.Should().BeTrue();
        oldPrimary.IsPrimary.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Fails_WhenNoCurrentPrimaryExists()
    {
        var tenantId = Guid.NewGuid();
        var employment = TestEmployment.Create(tenantId);
        employment.MarkSecondary();
        _repository.GetByIdAsync(employment.Id, Arg.Any<CancellationToken>()).Returns(employment);
        _repository.GetActivePrimaryEmploymentAsync(tenantId, employment.EmployeeId, Arg.Any<CancellationToken>())
            .Returns((Hris.Modules.Employment.Domain.Employment?)null);

        var result = await _handler.Handle(
            new ChangePrimaryEmploymentCommand(employment.Id.Value, tenantId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentNotFound);
    }
}
