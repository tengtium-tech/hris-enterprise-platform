using FluentAssertions;
using Hris.Modules.Administration.Domain;
using Xunit;

namespace Hris.Modules.Administration.Tests.Domain;

public sealed class AdministrativeDelegationTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly DateOnly _periodStart = DateOnly.FromDateTime(TestUserAccount.NowUtc.UtcDateTime);
    private static readonly DateOnly _periodEnd = _periodStart.AddDays(14);

    private static IReadOnlyList<DelegatedAuthorityItem> OneItem() => [new DelegatedAuthorityItem(CanonicalRole.HRManager, ScopeLevel.Tenant, null)];

    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var result = AdministrativeDelegation.Create(
            new DelegationId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), Guid.NewGuid(), OneItem(), _periodStart, _periodEnd,
            "Covering leave", null, false, false, TestUserAccount.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(DelegationStatus.Scheduled);
    }

    [Fact]
    public void Create_DelegatorEqualsDelegate_Fails()
    {
        var accountId = Guid.NewGuid();

        var result = AdministrativeDelegation.Create(
            new DelegationId(Guid.NewGuid()), _tenantId, accountId, accountId, OneItem(), _periodStart, _periodEnd, "Reason", null,
            false, false, TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.DelegatorEqualsDelegate);
    }

    [Fact]
    public void Create_EmptyAuthority_Fails()
    {
        var result = AdministrativeDelegation.Create(
            new DelegationId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), Guid.NewGuid(), [], _periodStart, _periodEnd, "Reason", null,
            false, false, TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.DelegatedAuthorityEmpty);
    }

    [Fact]
    public void Create_AuthorityExceedsDelegatorHoldings_Fails()
    {
        var result = AdministrativeDelegation.Create(
            new DelegationId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), Guid.NewGuid(), OneItem(), _periodStart, _periodEnd,
            "Reason", null, authorityExceedsDelegatorHoldings: true, violatesDelegateSeparationOfDuties: false, TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.DelegatedAuthorityExceedsDelegatorHoldings);
    }

    [Fact]
    public void Create_ViolatesSeparationOfDuties_Fails()
    {
        var result = AdministrativeDelegation.Create(
            new DelegationId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), Guid.NewGuid(), OneItem(), _periodStart, _periodEnd,
            "Reason", null, authorityExceedsDelegatorHoldings: false, violatesDelegateSeparationOfDuties: true, TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.DelegationSeparationOfDutiesViolation);
    }

    [Fact]
    public void Create_WithoutReason_Fails()
    {
        var result = AdministrativeDelegation.Create(
            new DelegationId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), Guid.NewGuid(), OneItem(), _periodStart, _periodEnd, null,
            null, false, false, TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.GrantReasonRequired);
    }

    [Fact]
    public void Create_EndBeforeStart_Fails()
    {
        var result = AdministrativeDelegation.Create(
            new DelegationId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), Guid.NewGuid(), OneItem(), _periodEnd, _periodStart, "Reason",
            null, false, false, TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Activate_FromScheduled_Succeeds()
    {
        var delegation = AdministrativeDelegation.Create(
            new DelegationId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), Guid.NewGuid(), OneItem(), _periodStart, _periodEnd,
            "Covering leave", null, false, false, TestUserAccount.NowUtc).Value;

        var result = delegation.Activate(false, false, TestUserAccount.NowUtc);

        result.IsSuccess.Should().BeTrue();
        delegation.Status.Should().Be(DelegationStatus.Active);
    }

    [Fact]
    public void Activate_WhenNotScheduled_Fails()
    {
        var delegation = AdministrativeDelegation.Create(
            new DelegationId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), Guid.NewGuid(), OneItem(), _periodStart, _periodEnd,
            "Covering leave", null, false, false, TestUserAccount.NowUtc).Value;
        delegation.Activate(false, false, TestUserAccount.NowUtc);

        var result = delegation.Activate(false, false, TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.DelegationNotScheduled);
    }

    [Fact]
    public void Activate_RevalidatesAuthority_Fails()
    {
        var delegation = AdministrativeDelegation.Create(
            new DelegationId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), Guid.NewGuid(), OneItem(), _periodStart, _periodEnd,
            "Covering leave", null, false, false, TestUserAccount.NowUtc).Value;

        var result = delegation.Activate(true, false, TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.DelegatedAuthorityExceedsDelegatorHoldings);
    }

    [Fact]
    public void Activate_RevalidatesSeparationOfDuties_Fails()
    {
        var delegation = AdministrativeDelegation.Create(
            new DelegationId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), Guid.NewGuid(), OneItem(), _periodStart, _periodEnd,
            "Covering leave", null, false, false, TestUserAccount.NowUtc).Value;

        var result = delegation.Activate(false, true, TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.DelegationSeparationOfDutiesViolation);
    }

    [Fact]
    public void Revoke_WhenExpired_IsIdempotent()
    {
        var delegation = AdministrativeDelegation.Create(
            new DelegationId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), Guid.NewGuid(), OneItem(), _periodStart, _periodEnd,
            "Covering leave", null, false, false, TestUserAccount.NowUtc).Value;
        delegation.Activate(false, false, TestUserAccount.NowUtc);
        delegation.Expire(TestUserAccount.NowUtc);

        var result = delegation.Revoke(Guid.NewGuid(), "Too late", TestUserAccount.NowUtc);

        result.IsSuccess.Should().BeTrue();
        delegation.Status.Should().Be(DelegationStatus.Expired);
    }

    [Fact]
    public void Expire_FromActive_Succeeds()
    {
        var delegation = AdministrativeDelegation.Create(
            new DelegationId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), Guid.NewGuid(), OneItem(), _periodStart, _periodEnd,
            "Covering leave", null, false, false, TestUserAccount.NowUtc).Value;
        delegation.Activate(false, false, TestUserAccount.NowUtc);

        var result = delegation.Expire(TestUserAccount.NowUtc);

        result.IsSuccess.Should().BeTrue();
        delegation.Status.Should().Be(DelegationStatus.Expired);
    }

    [Fact]
    public void Expire_WhenNotActive_Fails()
    {
        var delegation = AdministrativeDelegation.Create(
            new DelegationId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), Guid.NewGuid(), OneItem(), _periodStart, _periodEnd,
            "Covering leave", null, false, false, TestUserAccount.NowUtc).Value;

        var result = delegation.Expire(TestUserAccount.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AdministrationErrors.DelegationNotActive);
    }

    [Fact]
    public void Revoke_FromScheduled_Succeeds()
    {
        var delegation = AdministrativeDelegation.Create(
            new DelegationId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), Guid.NewGuid(), OneItem(), _periodStart, _periodEnd,
            "Covering leave", null, false, false, TestUserAccount.NowUtc).Value;

        var result = delegation.Revoke(Guid.NewGuid(), "No longer needed", TestUserAccount.NowUtc);

        result.IsSuccess.Should().BeTrue();
        delegation.Status.Should().Be(DelegationStatus.Revoked);
    }

    [Fact]
    public void Revoke_WhenAlreadyTerminal_IsIdempotent()
    {
        var delegation = AdministrativeDelegation.Create(
            new DelegationId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), Guid.NewGuid(), OneItem(), _periodStart, _periodEnd,
            "Covering leave", null, false, false, TestUserAccount.NowUtc).Value;
        delegation.Revoke(Guid.NewGuid(), "First", TestUserAccount.NowUtc);

        var result = delegation.Revoke(Guid.NewGuid(), "Second", TestUserAccount.NowUtc);

        result.IsSuccess.Should().BeTrue();
    }
}
