using FluentAssertions;
using Hris.Foundation.Entitlement.Domain;
using Xunit;

namespace Hris.Foundation.Entitlement.Tests.Domain;

/// <summary>
/// entitlement-framework.md's own Entitlement Evaluation diagram, exercised
/// end to end: Core short-circuit (CTR-ENT-008), pack-not-active denial, maturity-
/// insufficient denial, and the two paths to Entitled.
/// </summary>
public sealed class EntitlementEvaluatorTests
{
    [Fact]
    public void Evaluate_ReturnsEntitled_ForACorePack_RegardlessOfEditionOrRequiredMaturity()
    {
        var decision = EntitlementEvaluator.Evaluate(TenantEditionCode.Starter, ProcessPackCode.Organization, MaturityLevel.Advanced);

        decision.IsEntitled.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_ReturnsPackNotActive_WhenTheEditionDoesNotIncludeTheOptionalPackAtAll()
    {
        var decision = EntitlementEvaluator.Evaluate(TenantEditionCode.Starter, ProcessPackCode.Benefits, MaturityLevel.Essential);

        decision.IsEntitled.Should().BeFalse();
        decision.DenialReason.Should().Be(EntitlementDenialReason.PackNotActive);
    }

    [Fact]
    public void Evaluate_ReturnsMaturityLevelInsufficient_WhenThePackIsActiveButBelowTheRequiredLevel()
    {
        var decision = EntitlementEvaluator.Evaluate(TenantEditionCode.Starter, ProcessPackCode.Payroll, MaturityLevel.Standard);

        decision.IsEntitled.Should().BeFalse();
        decision.DenialReason.Should().Be(EntitlementDenialReason.MaturityLevelInsufficient);
    }

    [Fact]
    public void Evaluate_ReturnsEntitled_WhenThePacksDefaultMaturityExceedsTheRequiredLevel()
    {
        var decision = EntitlementEvaluator.Evaluate(TenantEditionCode.Enterprise, ProcessPackCode.Payroll, MaturityLevel.Essential);

        decision.IsEntitled.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_ReturnsEntitled_WhenThePacksDefaultMaturityExactlyMeetsTheRequiredLevel()
    {
        var decision = EntitlementEvaluator.Evaluate(TenantEditionCode.Growth, ProcessPackCode.Benefits, MaturityLevel.Essential);

        decision.IsEntitled.Should().BeTrue();
    }
}
