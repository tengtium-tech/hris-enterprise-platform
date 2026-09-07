using FluentAssertions;
using Hris.Modules.Organization.Application.Commands;
using Hris.Modules.Organization.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Organization.Tests.Application;

public sealed class CreateDepartmentCommandHandlerTests
{
    private readonly IOrganizationRepository _repository = Substitute.For<IOrganizationRepository>();
    private readonly CreateDepartmentCommandHandler _handler;

    public CreateDepartmentCommandHandlerTests()
    {
        _handler = new CreateDepartmentCommandHandler(_repository, new FakeTimeProvider(TestOrganization.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenCodeIsUniqueAndDivisionExists()
    {
        var organization = TestOrganization.Create();
        var businessUnitId = organization.AddBusinessUnit("Corporate", TestOrganization.NowUtc).Value;
        var divisionId = organization.AddDivision(businessUnitId, "Shared Services", TestOrganization.NowUtc).Value;
        _repository.ExistsDepartmentWithCodeAsync(organization.TenantId, "HR", null, Arg.Any<CancellationToken>()).Returns(false);
        _repository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);

        var result = await _handler.Handle(
            new CreateDepartmentCommand(organization.Id.Value, organization.TenantId, divisionId.Value, "Human Resources", "HR"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Fails_WhenCodeAlreadyExistsInTenant()
    {
        var organization = TestOrganization.Create();
        _repository.ExistsDepartmentWithCodeAsync(organization.TenantId, "HR", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(
            new CreateDepartmentCommand(organization.Id.Value, organization.TenantId, Guid.NewGuid(), "Human Resources", "HR"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.DuplicateDepartmentCode);
    }
}

public sealed class MoveDepartmentCommandHandlerTests
{
    private readonly IOrganizationRepository _repository = Substitute.For<IOrganizationRepository>();
    private readonly MoveDepartmentCommandHandler _handler;

    public MoveDepartmentCommandHandlerTests()
    {
        _handler = new MoveDepartmentCommandHandler(_repository, new FakeTimeProvider(TestOrganization.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenBothDivisionsExist()
    {
        var organization = TestOrganization.Create();
        var businessUnitId = organization.AddBusinessUnit("Corporate", TestOrganization.NowUtc).Value;
        var sourceDivisionId = organization.AddDivision(businessUnitId, "Shared Services", TestOrganization.NowUtc).Value;
        var targetDivisionId = organization.AddDivision(businessUnitId, "Operations", TestOrganization.NowUtc).Value;
        var departmentId = organization.AddDepartment(sourceDivisionId, "Human Resources", "HR", TestOrganization.NowUtc).Value;
        _repository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);

        var result = await _handler.Handle(
            new MoveDepartmentCommand(organization.Id.Value, organization.TenantId, departmentId.Value, targetDivisionId.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        organization.FindDepartment(departmentId)!.DivisionId.Should().Be(targetDivisionId);
    }
}

public sealed class MergeDepartmentsCommandHandlerTests
{
    private readonly IOrganizationRepository _repository = Substitute.For<IOrganizationRepository>();
    private readonly MergeDepartmentsCommandHandler _handler;

    public MergeDepartmentsCommandHandlerTests()
    {
        _handler = new MergeDepartmentsCommandHandler(_repository, new FakeTimeProvider(TestOrganization.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenSurvivingCodeIsUnique()
    {
        var organization = TestOrganization.Create();
        var businessUnitId = organization.AddBusinessUnit("Corporate", TestOrganization.NowUtc).Value;
        var divisionId = organization.AddDivision(businessUnitId, "Shared Services", TestOrganization.NowUtc).Value;
        var firstId = organization.AddDepartment(divisionId, "Payroll Ops", "PAY1", TestOrganization.NowUtc).Value;
        var secondId = organization.AddDepartment(divisionId, "Payroll Support", "PAY2", TestOrganization.NowUtc).Value;
        _repository.ExistsDepartmentWithCodeAsync(organization.TenantId, "PAY", null, Arg.Any<CancellationToken>()).Returns(false);
        _repository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);

        var result = await _handler.Handle(
            new MergeDepartmentsCommand(organization.Id.Value, organization.TenantId, [firstId.Value, secondId.Value], "Payroll", "PAY"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Fails_WhenSurvivingCodeAlreadyExists()
    {
        var organization = TestOrganization.Create();
        _repository.ExistsDepartmentWithCodeAsync(organization.TenantId, "PAY", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(
            new MergeDepartmentsCommand(
                organization.Id.Value, organization.TenantId, [Guid.NewGuid(), Guid.NewGuid()], "Payroll", "PAY"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.DuplicateDepartmentCode);
    }
}

public sealed class SplitDepartmentCommandHandlerTests
{
    private readonly IOrganizationRepository _repository = Substitute.For<IOrganizationRepository>();
    private readonly SplitDepartmentCommandHandler _handler;

    public SplitDepartmentCommandHandlerTests()
    {
        _handler = new SplitDepartmentCommandHandler(_repository, new FakeTimeProvider(TestOrganization.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenEveryNewCodeIsUnique()
    {
        var organization = TestOrganization.Create();
        var businessUnitId = organization.AddBusinessUnit("Corporate", TestOrganization.NowUtc).Value;
        var divisionId = organization.AddDivision(businessUnitId, "Shared Services", TestOrganization.NowUtc).Value;
        var sourceId = organization.AddDepartment(divisionId, "Human Resources", "HR", TestOrganization.NowUtc).Value;
        _repository.ExistsDepartmentWithCodeAsync(organization.TenantId, "REC", null, Arg.Any<CancellationToken>()).Returns(false);
        _repository.ExistsDepartmentWithCodeAsync(organization.TenantId, "ERL", null, Arg.Any<CancellationToken>()).Returns(false);
        _repository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);

        var result = await _handler.Handle(
            new SplitDepartmentCommand(
                organization.Id.Value, organization.TenantId, sourceId.Value,
                [new NewDepartmentSpec("Recruitment", "REC"), new NewDepartmentSpec("Employee Relations", "ERL")]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_Fails_WhenANewCodeAlreadyExists()
    {
        var organization = TestOrganization.Create();
        _repository.ExistsDepartmentWithCodeAsync(organization.TenantId, "REC", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(
            new SplitDepartmentCommand(
                organization.Id.Value, organization.TenantId, Guid.NewGuid(),
                [new NewDepartmentSpec("Recruitment", "REC"), new NewDepartmentSpec("Employee Relations", "ERL")]),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.DuplicateDepartmentCode);
    }
}

public sealed class ArchiveDepartmentCommandHandlerTests
{
    private readonly IOrganizationRepository _repository = Substitute.For<IOrganizationRepository>();
    private readonly ArchiveDepartmentCommandHandler _handler;

    public ArchiveDepartmentCommandHandlerTests()
    {
        _handler = new ArchiveDepartmentCommandHandler(_repository, new FakeTimeProvider(TestOrganization.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenDepartmentExists()
    {
        var organization = TestOrganization.Create();
        var businessUnitId = organization.AddBusinessUnit("Corporate", TestOrganization.NowUtc).Value;
        var divisionId = organization.AddDivision(businessUnitId, "Shared Services", TestOrganization.NowUtc).Value;
        var departmentId = organization.AddDepartment(divisionId, "Human Resources", "HR", TestOrganization.NowUtc).Value;
        _repository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);

        var result = await _handler.Handle(
            new ArchiveDepartmentCommand(organization.Id.Value, organization.TenantId, departmentId.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}

public sealed class RestoreDepartmentCommandHandlerTests
{
    private readonly IOrganizationRepository _repository = Substitute.For<IOrganizationRepository>();
    private readonly RestoreDepartmentCommandHandler _handler;

    public RestoreDepartmentCommandHandlerTests()
    {
        _handler = new RestoreDepartmentCommandHandler(_repository, new FakeTimeProvider(TestOrganization.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenDepartmentIsArchived()
    {
        var organization = TestOrganization.Create();
        var businessUnitId = organization.AddBusinessUnit("Corporate", TestOrganization.NowUtc).Value;
        var divisionId = organization.AddDivision(businessUnitId, "Shared Services", TestOrganization.NowUtc).Value;
        var departmentId = organization.AddDepartment(divisionId, "Human Resources", "HR", TestOrganization.NowUtc).Value;
        organization.ArchiveDepartment(departmentId, TestOrganization.NowUtc);
        _repository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);

        var result = await _handler.Handle(
            new RestoreDepartmentCommand(organization.Id.Value, organization.TenantId, departmentId.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}

public sealed class RenameDepartmentCommandHandlerTests
{
    private readonly IOrganizationRepository _repository = Substitute.For<IOrganizationRepository>();
    private readonly RenameDepartmentCommandHandler _handler;

    public RenameDepartmentCommandHandlerTests()
    {
        _handler = new RenameDepartmentCommandHandler(_repository, new FakeTimeProvider(TestOrganization.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenDepartmentExists()
    {
        var organization = TestOrganization.Create();
        var businessUnitId = organization.AddBusinessUnit("Corporate", TestOrganization.NowUtc).Value;
        var divisionId = organization.AddDivision(businessUnitId, "Shared Services", TestOrganization.NowUtc).Value;
        var departmentId = organization.AddDepartment(divisionId, "Human Resources", "HR", TestOrganization.NowUtc).Value;
        _repository.GetByIdAsync(organization.Id, Arg.Any<CancellationToken>()).Returns(organization);

        var result = await _handler.Handle(
            new RenameDepartmentCommand(organization.Id.Value, organization.TenantId, departmentId.Value, "Renamed"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
