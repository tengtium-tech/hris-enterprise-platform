using FluentAssertions;
using Hris.Foundation.Authorization.Domain;
using Xunit;

namespace Hris.Foundation.Authorization.Tests.Domain;

public sealed class AuthorizationDecisionTests
{
    [Fact]
    public void Allow_SetsIsAllowedTrue_WithNoDenialReason_AndCarriesAuthorizationEvaluatedEvent()
    {
        var principalId = TestData.NewPrincipalId();
        var permission = TestData.Permission();

        var decision = AuthorizationDecision.Allow(principalId, permission, TestData.NowUtc);

        decision.IsAllowed.Should().BeTrue();
        decision.DenialReason.Should().BeNull();
        decision.Event.Should().BeOfType<AuthorizationEvaluated>().Which.Should().BeEquivalentTo(new
        {
            PrincipalId = principalId,
            RequestedPermission = permission,
            IsAllowed = true,
        });
    }

    [Fact]
    public void Deny_SetsIsAllowedFalse_WithTheGivenReason_AndCarriesAuthorizationDeniedEvent()
    {
        var principalId = TestData.NewPrincipalId();
        var permission = TestData.Permission();
        const string reason = "The principal holds no effective role assignment.";

        var decision = AuthorizationDecision.Deny(principalId, permission, reason, TestData.NowUtc);

        decision.IsAllowed.Should().BeFalse();
        decision.DenialReason.Should().Be(reason);
        decision.Event.Should().BeOfType<AuthorizationDenied>().Which.Should().BeEquivalentTo(new
        {
            PrincipalId = principalId,
            RequestedPermission = permission,
            Reason = reason,
        });
    }
}
