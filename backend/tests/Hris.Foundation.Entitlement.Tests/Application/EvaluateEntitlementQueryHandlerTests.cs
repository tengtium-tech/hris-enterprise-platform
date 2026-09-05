using FluentAssertions;
using Hris.Foundation.Entitlement.Application.Queries;
using Hris.Foundation.Entitlement.Domain;
using Xunit;

namespace Hris.Foundation.Entitlement.Tests.Application;

/// <summary>
/// <see cref="EntitlementEvaluator"/>'s own tests already cover every branch of the
/// decision logic; these confirm the handler's own job -- translating the query into
/// the evaluator's own inputs and the decision into its own DTO.
/// </summary>
public sealed class EvaluateEntitlementQueryHandlerTests
{
    private readonly EvaluateEntitlementQueryHandler _handler = new();

    [Fact]
    public async Task Handle_ReturnsAnEntitledDto_WithNoDenialReason_ForACorePack()
    {
        var query = new EvaluateEntitlementQuery(TenantEditionCode.Starter, ProcessPackCode.Employee, MaturityLevel.Advanced);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsEntitled.Should().BeTrue();
        result.Value.DenialReason.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ReturnsADeniedDto_WithPackNotActiveReason_WhenTheEditionDoesNotHoldThePack()
    {
        var query = new EvaluateEntitlementQuery(TenantEditionCode.Starter, ProcessPackCode.Analytics, MaturityLevel.Essential);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsEntitled.Should().BeFalse();
        result.Value.DenialReason.Should().Be(nameof(EntitlementDenialReason.PackNotActive));
    }

    [Fact]
    public async Task Handle_ReturnsADeniedDto_WithMaturityLevelInsufficientReason_WhenThePackIsBelowTheRequiredLevel()
    {
        var query = new EvaluateEntitlementQuery(TenantEditionCode.Starter, ProcessPackCode.Leave, MaturityLevel.Advanced);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsEntitled.Should().BeFalse();
        result.Value.DenialReason.Should().Be(nameof(EntitlementDenialReason.MaturityLevelInsufficient));
    }
}
