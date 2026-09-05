using FluentAssertions;
using Hris.Foundation.Caching.Domain;
using Hris.Foundation.Caching.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Hris.Foundation.Caching.Tests.Infrastructure;

/// <summary>
/// Against a real, plain <see cref="MemoryCache"/> instance -- the genuine
/// correctness property under test is the token-based group-eviction mechanism
/// <see cref="MemoryCacheProvider"/>'s own remarks describe, which no fake
/// <see cref="ICacheProvider"/> could exercise meaningfully.
/// </summary>
public sealed class MemoryCacheProviderTests : IDisposable
{
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());
    private readonly MemoryCacheProvider _sut;

    public MemoryCacheProviderTests()
    {
        _sut = new MemoryCacheProvider(_memoryCache);
    }

    public void Dispose() => _memoryCache.Dispose();

    [Fact]
    public async Task GetAsync_ReturnsNull_ForAKeyNeverSet()
    {
        var key = CacheKey.Create(Guid.NewGuid(), "Employees", "employee:1").Value;

        var result = await _sut.GetAsync(key, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_RoundTripsTheStoredValue()
    {
        var key = CacheKey.Create(Guid.NewGuid(), "Employees", "employee:1").Value;

        await _sut.SetAsync(key, "ada-lovelace", CacheEntryOptions.NeverExpire, CancellationToken.None);
        var result = await _sut.GetAsync(key, CancellationToken.None);

        result.Should().Be("ada-lovelace");
    }

    [Fact]
    public async Task RemoveAsync_RemovesOnlyTheGivenKey()
    {
        var tenantId = Guid.NewGuid();
        var target = CacheKey.Create(tenantId, "Employees", "employee:1").Value;
        var other = CacheKey.Create(tenantId, "Employees", "employee:2").Value;
        await _sut.SetAsync(target, "value-1", CacheEntryOptions.NeverExpire, CancellationToken.None);
        await _sut.SetAsync(other, "value-2", CacheEntryOptions.NeverExpire, CancellationToken.None);

        await _sut.RemoveAsync(target, CancellationToken.None);

        (await _sut.GetAsync(target, CancellationToken.None)).Should().BeNull();
        (await _sut.GetAsync(other, CancellationToken.None)).Should().Be("value-2");
    }

    [Fact]
    public async Task ClearRegionAsync_EvictsEveryEntryInThatRegion_ForThatTenant()
    {
        var tenantId = Guid.NewGuid();
        var first = CacheKey.Create(tenantId, "Employees", "employee:1").Value;
        var second = CacheKey.Create(tenantId, "Employees", "employee:2").Value;
        await _sut.SetAsync(first, "value-1", CacheEntryOptions.NeverExpire, CancellationToken.None);
        await _sut.SetAsync(second, "value-2", CacheEntryOptions.NeverExpire, CancellationToken.None);

        await _sut.ClearRegionAsync(tenantId, "Employees", CancellationToken.None);

        (await _sut.GetAsync(first, CancellationToken.None)).Should().BeNull();
        (await _sut.GetAsync(second, CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task ClearRegionAsync_NeverEvictsADifferentRegion_ForTheSameTenant()
    {
        var tenantId = Guid.NewGuid();
        var employeeEntry = CacheKey.Create(tenantId, "Employees", "employee:1").Value;
        var payrollEntry = CacheKey.Create(tenantId, "Payroll", "payroll:1").Value;
        await _sut.SetAsync(employeeEntry, "value-1", CacheEntryOptions.NeverExpire, CancellationToken.None);
        await _sut.SetAsync(payrollEntry, "value-2", CacheEntryOptions.NeverExpire, CancellationToken.None);

        await _sut.ClearRegionAsync(tenantId, "Employees", CancellationToken.None);

        (await _sut.GetAsync(employeeEntry, CancellationToken.None)).Should().BeNull();
        (await _sut.GetAsync(payrollEntry, CancellationToken.None)).Should().Be("value-2");
    }

    [Fact]
    public async Task ClearRegionAsync_NeverEvictsTheSameRegion_ForADifferentTenant()
    {
        var firstTenantId = Guid.NewGuid();
        var secondTenantId = Guid.NewGuid();
        var firstTenantEntry = CacheKey.Create(firstTenantId, "Employees", "employee:1").Value;
        var secondTenantEntry = CacheKey.Create(secondTenantId, "Employees", "employee:1").Value;
        await _sut.SetAsync(firstTenantEntry, "value-1", CacheEntryOptions.NeverExpire, CancellationToken.None);
        await _sut.SetAsync(secondTenantEntry, "value-2", CacheEntryOptions.NeverExpire, CancellationToken.None);

        await _sut.ClearRegionAsync(firstTenantId, "Employees", CancellationToken.None);

        (await _sut.GetAsync(firstTenantEntry, CancellationToken.None)).Should().BeNull();
        (await _sut.GetAsync(secondTenantEntry, CancellationToken.None)).Should().Be("value-2");
    }

    [Fact]
    public async Task ClearTenantAsync_EvictsEveryEntry_AcrossEveryRegion_ForThatTenant()
    {
        var tenantId = Guid.NewGuid();
        var employeeEntry = CacheKey.Create(tenantId, "Employees", "employee:1").Value;
        var payrollEntry = CacheKey.Create(tenantId, "Payroll", "payroll:1").Value;
        await _sut.SetAsync(employeeEntry, "value-1", CacheEntryOptions.NeverExpire, CancellationToken.None);
        await _sut.SetAsync(payrollEntry, "value-2", CacheEntryOptions.NeverExpire, CancellationToken.None);

        await _sut.ClearTenantAsync(tenantId, CancellationToken.None);

        (await _sut.GetAsync(employeeEntry, CancellationToken.None)).Should().BeNull();
        (await _sut.GetAsync(payrollEntry, CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task ClearTenantAsync_NeverEvictsADifferentTenant()
    {
        var firstTenantId = Guid.NewGuid();
        var secondTenantId = Guid.NewGuid();
        var firstTenantEntry = CacheKey.Create(firstTenantId, "Employees", "employee:1").Value;
        var secondTenantEntry = CacheKey.Create(secondTenantId, "Employees", "employee:1").Value;
        await _sut.SetAsync(firstTenantEntry, "value-1", CacheEntryOptions.NeverExpire, CancellationToken.None);
        await _sut.SetAsync(secondTenantEntry, "value-2", CacheEntryOptions.NeverExpire, CancellationToken.None);

        await _sut.ClearTenantAsync(firstTenantId, CancellationToken.None);

        (await _sut.GetAsync(firstTenantEntry, CancellationToken.None)).Should().BeNull();
        (await _sut.GetAsync(secondTenantEntry, CancellationToken.None)).Should().Be("value-2");
    }

    [Fact]
    public async Task AfterClearingARegion_ANewEntryInTheSameRegion_SurvivesNormally()
    {
        var tenantId = Guid.NewGuid();
        var region = "Employees";
        var firstEntry = CacheKey.Create(tenantId, region, "employee:1").Value;
        await _sut.SetAsync(firstEntry, "value-1", CacheEntryOptions.NeverExpire, CancellationToken.None);
        await _sut.ClearRegionAsync(tenantId, region, CancellationToken.None);

        var secondEntry = CacheKey.Create(tenantId, region, "employee:2").Value;
        await _sut.SetAsync(secondEntry, "value-2", CacheEntryOptions.NeverExpire, CancellationToken.None);

        (await _sut.GetAsync(secondEntry, CancellationToken.None)).Should().Be("value-2");
    }

    [Fact]
    public async Task GetStatistics_CountsHitsAndMisses()
    {
        var key = CacheKey.Create(Guid.NewGuid(), "Employees", "employee:1").Value;
        await _sut.SetAsync(key, "value-1", CacheEntryOptions.NeverExpire, CancellationToken.None);

        await _sut.GetAsync(key, CancellationToken.None);
        await _sut.GetAsync(CacheKey.Create(Guid.NewGuid(), "Employees", "missing").Value, CancellationToken.None);

        var statistics = _sut.GetStatistics();

        statistics.Hits.Should().Be(1);
        statistics.Misses.Should().Be(1);
        statistics.HitRatio.Should().Be(0.5);
    }
}
