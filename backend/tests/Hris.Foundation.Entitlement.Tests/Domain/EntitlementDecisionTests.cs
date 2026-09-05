using FluentAssertions;
using Hris.Foundation.Entitlement.Domain;
using Xunit;

namespace Hris.Foundation.Entitlement.Tests.Domain;

public sealed class EntitlementDecisionTests
{
    [Fact]
    public void Entitled_HasNoDenialReason()
    {
        var decision = EntitlementDecision.Entitled();

        decision.IsEntitled.Should().BeTrue();
        decision.DenialReason.Should().BeNull();
    }

    [Theory]
    [InlineData(EntitlementDenialReason.PackNotActive)]
    [InlineData(EntitlementDenialReason.MaturityLevelInsufficient)]
    public void Denied_CarriesTheGivenReason(EntitlementDenialReason reason)
    {
        var decision = EntitlementDecision.Denied(reason);

        decision.IsEntitled.Should().BeFalse();
        decision.DenialReason.Should().Be(reason);
    }
}
