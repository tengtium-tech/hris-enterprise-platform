using FluentAssertions;
using Hris.Foundation.Identity.Domain;
using Hris.Foundation.Tenant.Application.Queries;
using Hris.Foundation.Tenant.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Tenant.Tests.Application;

public sealed class GetPlatformDashboardSummaryQueryHandlerTests
{
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly IUserAccountRepository _userAccountRepository = Substitute.For<IUserAccountRepository>();
    private readonly GetPlatformDashboardSummaryQueryHandler _handler;

    public GetPlatformDashboardSummaryQueryHandlerTests()
    {
        _handler = new GetPlatformDashboardSummaryQueryHandler(_tenantRepository, _userAccountRepository);
    }

    [Fact]
    public async Task Handle_ReturnsPlatformWideAggregateCounts_WithNoPerTenantBreakdown()
    {
        IReadOnlyDictionary<TenantLifecycleState, int> byState =
            new Dictionary<TenantLifecycleState, int> { [TenantLifecycleState.Active] = 5, [TenantLifecycleState.Suspended] = 1 };
        IReadOnlyDictionary<SubscriptionPlan, int> byPlan =
            new Dictionary<SubscriptionPlan, int> { [SubscriptionPlan.Growth] = 4, [SubscriptionPlan.Enterprise] = 2 };

        _tenantRepository.CountByLifecycleStateAsync(Arg.Any<CancellationToken>()).Returns(byState);
        _tenantRepository.CountBySubscriptionPlanAsync(Arg.Any<CancellationToken>()).Returns(byPlan);
        _userAccountRepository.CountAllAsync(Arg.Any<CancellationToken>()).Returns(42);

        var result = await _handler.Handle(new GetPlatformDashboardSummaryQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantCountByLifecycleState.Should().Contain(new KeyValuePair<string, int>("Active", 5));
        result.Value.TenantCountByLifecycleState.Should().Contain(new KeyValuePair<string, int>("Suspended", 1));
        result.Value.TenantCountBySubscriptionPlan.Should().Contain(new KeyValuePair<string, int>("Growth", 4));
        result.Value.TenantCountBySubscriptionPlan.Should().Contain(new KeyValuePair<string, int>("Enterprise", 2));
        result.Value.TotalUserAccountCount.Should().Be(42);
    }
}
