using FluentAssertions;
using Hris.Foundation.Tenant.Application.Commands;
using Hris.Foundation.Tenant.Domain;
using NSubstitute;
using Xunit;
using TenantAggregate = Hris.Foundation.Tenant.Domain.Tenant;

namespace Hris.Foundation.Tenant.Tests.Application;

public sealed class SuspendTenantCommandHandlerTests
{
    private readonly ITenantRepository _repository = Substitute.For<ITenantRepository>();
    private readonly SuspendTenantCommandHandler _handler;

    public SuspendTenantCommandHandlerTests()
    {
        _handler = new SuspendTenantCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenTenantExists()
    {
        var tenant = TestData.ActiveTenant();
        _repository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var result = await _handler.Handle(
            new SuspendTenantCommand(tenant.Id.Value, "Non-payment", Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tenant.LifecycleState.Should().Be(TenantLifecycleState.Suspended);
    }

    [Fact]
    public async Task Handle_Fails_WhenTenantDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>()).Returns((TenantAggregate?)null);

        var result = await _handler.Handle(
            new SuspendTenantCommand(Guid.NewGuid(), "Reason", Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.TenantNotFound);
    }
}

public sealed class ReactivateTenantCommandHandlerTests
{
    private readonly ITenantRepository _repository = Substitute.For<ITenantRepository>();
    private readonly ReactivateTenantCommandHandler _handler;

    public ReactivateTenantCommandHandlerTests()
    {
        _handler = new ReactivateTenantCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenTenantExists()
    {
        var tenant = TestData.SuspendedTenant();
        _repository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var result = await _handler.Handle(
            new ReactivateTenantCommand(tenant.Id.Value, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tenant.LifecycleState.Should().Be(TenantLifecycleState.Active);
    }

    [Fact]
    public async Task Handle_Fails_WhenTenantDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>()).Returns((TenantAggregate?)null);

        var result = await _handler.Handle(
            new ReactivateTenantCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.TenantNotFound);
    }
}

public sealed class ArchiveTenantCommandHandlerTests
{
    private readonly ITenantRepository _repository = Substitute.For<ITenantRepository>();
    private readonly ArchiveTenantCommandHandler _handler;

    public ArchiveTenantCommandHandlerTests()
    {
        _handler = new ArchiveTenantCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenTenantExists()
    {
        var tenant = TestData.ActiveTenant();
        _repository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var result = await _handler.Handle(
            new ArchiveTenantCommand(tenant.Id.Value, "Churned", Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tenant.LifecycleState.Should().Be(TenantLifecycleState.Archived);
    }

    [Fact]
    public async Task Handle_Fails_WhenTenantDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>()).Returns((TenantAggregate?)null);

        var result = await _handler.Handle(
            new ArchiveTenantCommand(Guid.NewGuid(), "Reason", Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.TenantNotFound);
    }
}

public sealed class DeleteTenantCommandHandlerTests
{
    private readonly ITenantRepository _repository = Substitute.For<ITenantRepository>();
    private readonly DeleteTenantCommandHandler _handler;

    public DeleteTenantCommandHandlerTests()
    {
        _handler = new DeleteTenantCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenTenantIsArchived()
    {
        var tenant = TestData.ArchivedTenant();
        _repository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var result = await _handler.Handle(
            new DeleteTenantCommand(tenant.Id.Value, "Retention elapsed", "RA 10173 request", Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tenant.LifecycleState.Should().Be(TenantLifecycleState.Deleted);
    }

    [Fact]
    public async Task Handle_Fails_WhenTenantIsNotArchived()
    {
        var tenant = TestData.ActiveTenant();
        _repository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        var result = await _handler.Handle(
            new DeleteTenantCommand(tenant.Id.Value, "Reason", "Basis", Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.DeleteRequiresArchived);
    }

    [Fact]
    public async Task Handle_Fails_WhenTenantDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>()).Returns((TenantAggregate?)null);

        var result = await _handler.Handle(
            new DeleteTenantCommand(Guid.NewGuid(), "Reason", "Basis", Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.TenantNotFound);
    }
}
