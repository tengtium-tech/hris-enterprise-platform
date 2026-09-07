using FluentAssertions;
using Hris.Modules.Organization.Application.Commands;
using Hris.Modules.Organization.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Organization.Tests.Application;

public sealed class CreateWorkLocationCommandHandlerTests
{
    private readonly IWorkLocationRepository _repository = Substitute.For<IWorkLocationRepository>();
    private readonly CreateWorkLocationCommandHandler _handler;

    public CreateWorkLocationCommandHandlerTests()
    {
        _handler = new CreateWorkLocationCommandHandler(_repository, new FakeTimeProvider(TestOrganization.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenCodeIsUniqueAndDataIsValid()
    {
        var tenantId = Guid.NewGuid();
        _repository.ExistsWithCodeAsync(tenantId, "HQ", null, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(
            new CreateWorkLocationCommand(
                tenantId, "HQ", "Headquarters", Guid.NewGuid(), null, "123 Ayala Ave", null, "Makati", "Metro Manila",
                "1226", "Philippines", "Asia/Manila", null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<WorkLocation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenCodeAlreadyExists()
    {
        var tenantId = Guid.NewGuid();
        _repository.ExistsWithCodeAsync(tenantId, "HQ", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(
            new CreateWorkLocationCommand(
                tenantId, "HQ", "Headquarters", Guid.NewGuid(), null, "123 Ayala Ave", null, "Makati", "Metro Manila",
                "1226", "Philippines", "Asia/Manila", null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.DuplicateLocationCode);
    }

    [Fact]
    public async Task Handle_Fails_WhenTimeZoneIsInvalid()
    {
        var tenantId = Guid.NewGuid();
        _repository.ExistsWithCodeAsync(tenantId, "HQ", null, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(
            new CreateWorkLocationCommand(
                tenantId, "HQ", "Headquarters", Guid.NewGuid(), null, "123 Ayala Ave", null, "Makati", "Metro Manila",
                "1226", "Philippines", "Not/AZone", null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.TimeZoneInvalid);
    }
}

public sealed class UpdateWorkLocationCommandHandlerTests
{
    private readonly IWorkLocationRepository _repository = Substitute.For<IWorkLocationRepository>();
    private readonly UpdateWorkLocationCommandHandler _handler;

    public UpdateWorkLocationCommandHandlerTests()
    {
        _handler = new UpdateWorkLocationCommandHandler(_repository, new FakeTimeProvider(TestOrganization.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenWorkLocationExists()
    {
        var workLocation = TestWorkLocation.Create();
        _repository.GetByIdAsync(workLocation.Id, Arg.Any<CancellationToken>()).Returns(workLocation);

        var result = await _handler.Handle(
            new UpdateWorkLocationCommand(
                workLocation.Id.Value, workLocation.TenantId, "Renamed", "456 New St", null, "Makati", "Metro Manila", "1226",
                "Philippines", "Asia/Manila", null, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        workLocation.Name.Should().Be("Renamed");
    }
}

public sealed class ArchiveWorkLocationCommandHandlerTests
{
    private readonly IWorkLocationRepository _repository = Substitute.For<IWorkLocationRepository>();
    private readonly ArchiveWorkLocationCommandHandler _handler;

    public ArchiveWorkLocationCommandHandlerTests()
    {
        _handler = new ArchiveWorkLocationCommandHandler(_repository, new FakeTimeProvider(TestOrganization.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenWorkLocationExists()
    {
        var workLocation = TestWorkLocation.Create();
        _repository.GetByIdAsync(workLocation.Id, Arg.Any<CancellationToken>()).Returns(workLocation);

        var result = await _handler.Handle(
            new ArchiveWorkLocationCommand(workLocation.Id.Value, workLocation.TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}

public sealed class RestoreWorkLocationCommandHandlerTests
{
    private readonly IWorkLocationRepository _repository = Substitute.For<IWorkLocationRepository>();
    private readonly RestoreWorkLocationCommandHandler _handler;

    public RestoreWorkLocationCommandHandlerTests()
    {
        _handler = new RestoreWorkLocationCommandHandler(_repository, new FakeTimeProvider(TestOrganization.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenWorkLocationIsArchived()
    {
        var workLocation = TestWorkLocation.Create();
        workLocation.Archive(TestOrganization.NowUtc);
        _repository.GetByIdAsync(workLocation.Id, Arg.Any<CancellationToken>()).Returns(workLocation);

        var result = await _handler.Handle(
            new RestoreWorkLocationCommand(workLocation.Id.Value, workLocation.TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}

internal static class TestWorkLocation
{
    public static WorkLocation Create()
    {
        var address = Address.Create("123 Ayala Ave", null, "Makati", "Metro Manila", "1226", "Philippines").Value;
        var timeZone = WorkLocationTimeZone.Create("Asia/Manila").Value;

        return WorkLocation.Create(
            new WorkLocationId(Guid.NewGuid()), Guid.NewGuid(), "HQ", "Headquarters", Guid.NewGuid(), null, address, timeZone,
            null, TestOrganization.NowUtc).Value;
    }
}
