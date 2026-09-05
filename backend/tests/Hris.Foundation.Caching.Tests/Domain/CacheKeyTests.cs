using FluentAssertions;
using Hris.Foundation.Caching.Domain;
using Xunit;

namespace Hris.Foundation.Caching.Tests.Domain;

public sealed class CacheKeyTests
{
    [Fact]
    public void Create_Succeeds_ForValidInputs()
    {
        var result = CacheKey.Create(Guid.NewGuid(), "Employees", "employee:100001");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_Fails_ForAnEmptyTenantId()
    {
        var result = CacheKey.Create(Guid.Empty, "Employees", "employee:100001");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CacheErrors.TenantIdRequired);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Fails_ForAMissingRegion(string? region)
    {
        var result = CacheKey.Create(Guid.NewGuid(), region, "employee:100001");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CacheErrors.RegionRequired);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Fails_ForAMissingKey(string? key)
    {
        var result = CacheKey.Create(Guid.NewGuid(), "Employees", key);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CacheErrors.KeyRequired);
    }

    [Fact]
    public void Create_TrimsRegionAndKey()
    {
        var result = CacheKey.Create(Guid.NewGuid(), "  Employees  ", "  employee:100001  ");

        result.Value.Region.Should().Be("Employees");
        result.Value.Key.Should().Be("employee:100001");
    }

    [Fact]
    public void TwoKeys_WithTheSameTenantRegionAndKey_AreEqual()
    {
        var tenantId = Guid.NewGuid();

        var first = CacheKey.Create(tenantId, "Employees", "employee:100001").Value;
        var second = CacheKey.Create(tenantId, "Employees", "employee:100001").Value;

        first.Should().Be(second);
        (first == second).Should().BeTrue();
    }

    [Fact]
    public void TwoKeys_WithDifferentTenants_AreNotEqual_EvenWithTheSameRegionAndKey()
    {
        var first = CacheKey.Create(Guid.NewGuid(), "Employees", "employee:100001").Value;
        var second = CacheKey.Create(Guid.NewGuid(), "Employees", "employee:100001").Value;

        first.Should().NotBe(second);
    }

    [Fact]
    public void ToString_ProducesADeterministicTenantScopedForm()
    {
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var key = CacheKey.Create(tenantId, "Employees", "employee:100001").Value;

        key.ToString().Should().Be("tenant:11111111-1111-1111-1111-111111111111:region:Employees:key:employee:100001");
    }
}
