using FluentAssertions;
using Hris.Modules.Leave.Domain;
using Xunit;

namespace Hris.Modules.Leave.Tests.Domain;

/// <summary>
/// LV-001 through LV-004: the catalog split between platform-provided statutory types
/// (seeded, never editable or deactivatable) and freely tenant-defined company types.
/// <see cref="LeaveType.SeedStatutoryType"/> is not reachable through any tenant-facing
/// command — it exists only for platform seed data — so it is tested directly here the
/// same way the aggregate itself is constructed, per
/// docs/04-modules/leave/domain/leave-types.md.
/// </summary>
public sealed class LeaveTypeTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    private LeaveType NewTenantType(string code = "BEREAVEMENT", string name = "Bereavement Leave") =>
        LeaveType.DefineTenantType(
            new LeaveTypeId(Guid.NewGuid()), _tenantId, code, name, Guid.NewGuid(), TestLeave.NowUtc).Value;

    private static LeaveType NewStatutoryType(
        string code = "SIL", string name = "Service Incentive Leave", string basis = "Labor Code Art. 95",
        decimal minimum = 5) =>
        LeaveType.SeedStatutoryType(new LeaveTypeId(Guid.NewGuid()), code, name, basis, minimum).Value;

    // ---- DefineTenantType (LV-002) ---------------------------------------

    [Fact]
    public void DefineTenantType_StartsActive_AsCompanyDefinedAndTenantScoped_AndRaisesDefinedEvent()
    {
        var actorId = Guid.NewGuid();
        var result = LeaveType.DefineTenantType(
            new LeaveTypeId(Guid.NewGuid()), _tenantId, "BEREAVEMENT", "Bereavement Leave", actorId, TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        var leaveType = result.Value;
        leaveType.Status.Should().Be(LeaveTypeStatus.Active);
        leaveType.Category.Should().Be(LeaveTypeCategory.CompanyDefined);
        leaveType.Scope.Should().Be(LeaveTypeScope.Tenant);
        leaveType.TenantId.Should().Be(_tenantId);
        leaveType.StatutoryBasis.Should().BeNull();
        leaveType.StatutoryMinimum.Should().BeNull();

        var raised = leaveType.DomainEvents.OfType<LeaveTypeDefined>().Single();
        raised.TenantId.Should().Be(_tenantId);
        raised.Code.Should().Be("BEREAVEMENT");
        raised.ActorId.Should().Be(actorId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DefineTenantType_Fails_WhenCodeIsMissing(string? code)
    {
        var result = LeaveType.DefineTenantType(
            new LeaveTypeId(Guid.NewGuid()), _tenantId, code, "Bereavement Leave", Guid.NewGuid(), TestLeave.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.LeaveTypeCodeRequired);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DefineTenantType_Fails_WhenNameIsMissing(string? name)
    {
        var result = LeaveType.DefineTenantType(
            new LeaveTypeId(Guid.NewGuid()), _tenantId, "BEREAVEMENT", name, Guid.NewGuid(), TestLeave.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.LeaveTypeNameRequired);
    }

    // ---- SeedStatutoryType (LV-001) ---------------------------------------

    [Fact]
    public void SeedStatutoryType_StartsActive_AsStatutoryAndPlatformScoped_AndRaisesNoEvent()
    {
        // domain-events.md scopes LeaveTypeDefined's own documented meaning to "a
        // tenant-defined leave type is created" -- platform seeding is not that.
        var result = LeaveType.SeedStatutoryType(
            new LeaveTypeId(Guid.NewGuid()), "MATERNITY", "Maternity Leave", "RA 11210", 105);

        result.IsSuccess.Should().BeTrue();
        var leaveType = result.Value;
        leaveType.Status.Should().Be(LeaveTypeStatus.Active);
        leaveType.Category.Should().Be(LeaveTypeCategory.Statutory);
        leaveType.Scope.Should().Be(LeaveTypeScope.Platform);
        leaveType.TenantId.Should().BeNull();
        leaveType.StatutoryBasis.Should().Be("RA 11210");
        leaveType.StatutoryMinimum.Should().Be(105);
        leaveType.DomainEvents.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SeedStatutoryType_Fails_WhenCodeIsMissing(string? code)
    {
        var result = LeaveType.SeedStatutoryType(new LeaveTypeId(Guid.NewGuid()), code, "Maternity Leave", "RA 11210", 105);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.LeaveTypeCodeRequired);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SeedStatutoryType_Fails_WhenNameIsMissing(string? name)
    {
        var result = LeaveType.SeedStatutoryType(new LeaveTypeId(Guid.NewGuid()), "MATERNITY", name, "RA 11210", 105);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.LeaveTypeNameRequired);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SeedStatutoryType_Fails_WhenStatutoryBasisIsMissing(string? basis)
    {
        var result = LeaveType.SeedStatutoryType(new LeaveTypeId(Guid.NewGuid()), "MATERNITY", "Maternity Leave", basis, 105);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.StatutoryBasisRequired);
    }

    [Fact]
    public void SeedStatutoryType_Fails_WhenStatutoryMinimumIsNegative()
    {
        var result = LeaveType.SeedStatutoryType(new LeaveTypeId(Guid.NewGuid()), "MATERNITY", "Maternity Leave", "RA 11210", -1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.StatutoryMinimumMustNotBeNegative);
    }

    [Fact]
    public void SeedStatutoryType_Succeeds_WhenStatutoryMinimumIsZero()
    {
        var result = LeaveType.SeedStatutoryType(new LeaveTypeId(Guid.NewGuid()), "MATERNITY", "Maternity Leave", "RA 11210", 0);

        result.IsSuccess.Should().BeTrue();
    }

    // ---- Deactivate (LV-003, LV-004) --------------------------------------

    [Fact]
    public void Deactivate_TransitionsToInactive_ForATenantDefinedType_AndRaisesDeactivatedEvent()
    {
        var leaveType = NewTenantType();
        var actorId = Guid.NewGuid();

        var result = leaveType.Deactivate(actorId, "no longer offered", TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        leaveType.Status.Should().Be(LeaveTypeStatus.Inactive);
        var raised = leaveType.DomainEvents.OfType<LeaveTypeDeactivated>().Single();
        raised.ActorId.Should().Be(actorId);
        raised.Reason.Should().Be("no longer offered");
    }

    [Fact]
    public void Deactivate_IsIdempotent_WhenAlreadyInactive()
    {
        var leaveType = NewTenantType();
        leaveType.Deactivate(Guid.NewGuid(), "first", TestLeave.NowUtc);
        leaveType.ClearDomainEvents();

        var result = leaveType.Deactivate(Guid.NewGuid(), "second", TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        leaveType.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Deactivate_Fails_ForAStatutoryType_RegardlessOfActorOrReason()
    {
        var leaveType = NewStatutoryType();

        var result = leaveType.Deactivate(Guid.NewGuid(), "attempted deactivation", TestLeave.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.CannotDeactivateStatutoryLeaveType);
        leaveType.Status.Should().Be(LeaveTypeStatus.Active, "LV-004: a statutory type is never available for deactivation");
    }
}
