using FluentAssertions;
using Hris.Foundation.Entitlement.Application.Queries;
using Hris.Foundation.Entitlement.Domain;
using Xunit;

namespace Hris.Foundation.Entitlement.Tests.Application;

public sealed class GetEditionEntitlementSummaryQueryHandlerTests
{
    private readonly GetEditionEntitlementSummaryQueryHandler _handler = new();

    [Fact]
    public async Task Handle_ReturnsAllTwentyOnePacks_ForTheQueriedEdition()
    {
        var result = await _handler.Handle(new GetEditionEntitlementSummaryQuery(TenantEditionCode.Growth), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(21);
    }

    [Fact]
    public async Task Handle_ReturnsEntitledWithNoMaturityLevel_ForACorePack()
    {
        var result = await _handler.Handle(new GetEditionEntitlementSummaryQuery(TenantEditionCode.Starter), CancellationToken.None);

        var organization = result.Value.Single(dto => dto.Code == nameof(ProcessPackCode.Organization));

        organization.IsCore.Should().BeTrue();
        organization.IsEntitled.Should().BeTrue();
        organization.MaturityLevel.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ReturnsNotEntitled_ForAnOptionalPackTheEditionDoesNotHold()
    {
        var result = await _handler.Handle(new GetEditionEntitlementSummaryQuery(TenantEditionCode.Starter), CancellationToken.None);

        var analytics = result.Value.Single(dto => dto.Code == nameof(ProcessPackCode.Analytics));

        analytics.IsCore.Should().BeFalse();
        analytics.IsEntitled.Should().BeFalse();
        analytics.MaturityLevel.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ReturnsEntitledWithItsMaturityLevel_ForAnOptionalPackTheEditionHolds()
    {
        var result = await _handler.Handle(new GetEditionEntitlementSummaryQuery(TenantEditionCode.Enterprise), CancellationToken.None);

        var payroll = result.Value.Single(dto => dto.Code == nameof(ProcessPackCode.Payroll));

        payroll.IsCore.Should().BeFalse();
        payroll.IsEntitled.Should().BeTrue();
        payroll.MaturityLevel.Should().Be(nameof(MaturityLevel.Advanced));
    }
}
