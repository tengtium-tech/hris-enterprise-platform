using FluentAssertions;
using Hris.Foundation.Caching.Application;
using Hris.Foundation.Caching.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Caching.Tests.Application;

/// <summary>
/// <see cref="MemoryCacheProviderTests"/> already covers <see cref="ICacheProvider"/>'s
/// own real behavior; these confirm <see cref="CacheService"/>'s own job -- JSON
/// serialization and delegation -- against a fake provider.
/// </summary>
public sealed class CacheServiceTests
{
    private sealed record EmployeeProfile(string FirstName, string LastName);

    private readonly ICacheProvider _provider = Substitute.For<ICacheProvider>();
    private readonly CacheService _sut;

    public CacheServiceTests()
    {
        _sut = new CacheService(_provider);
    }

    [Fact]
    public async Task GetAsync_ReturnsDefault_WhenTheProviderHasNoValue()
    {
        var key = CacheKey.Create(Guid.NewGuid(), "Employees", "employee:1").Value;
        _provider.GetAsync(key, Arg.Any<CancellationToken>()).Returns((string?)null);

        var result = await _sut.GetAsync<EmployeeProfile>(key, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_DeserializesTheProvidersOwnStoredJson()
    {
        var key = CacheKey.Create(Guid.NewGuid(), "Employees", "employee:1").Value;
        _provider.GetAsync(key, Arg.Any<CancellationToken>()).Returns("""{"FirstName":"Ada","LastName":"Lovelace"}""");

        var result = await _sut.GetAsync<EmployeeProfile>(key, CancellationToken.None);

        result.Should().Be(new EmployeeProfile("Ada", "Lovelace"));
    }

    [Fact]
    public async Task SetAsync_SerializesTheValue_AndPassesItToTheProvider()
    {
        var key = CacheKey.Create(Guid.NewGuid(), "Employees", "employee:1").Value;
        var value = new EmployeeProfile("Ada", "Lovelace");

        await _sut.SetAsync(key, value, CacheEntryOptions.NeverExpire, CancellationToken.None);

        await _provider.Received(1).SetAsync(
            key,
            Arg.Is<string>(json => json.Contains("Ada", StringComparison.Ordinal) && json.Contains("Lovelace", StringComparison.Ordinal)),
            CacheEntryOptions.NeverExpire,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveAsync_DelegatesToTheProvider()
    {
        var key = CacheKey.Create(Guid.NewGuid(), "Employees", "employee:1").Value;

        await _sut.RemoveAsync(key, CancellationToken.None);

        await _provider.Received(1).RemoveAsync(key, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClearRegionAsync_DelegatesToTheProvider()
    {
        var tenantId = Guid.NewGuid();

        await _sut.ClearRegionAsync(tenantId, "Employees", CancellationToken.None);

        await _provider.Received(1).ClearRegionAsync(tenantId, "Employees", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClearTenantAsync_DelegatesToTheProvider()
    {
        var tenantId = Guid.NewGuid();

        await _sut.ClearTenantAsync(tenantId, CancellationToken.None);

        await _provider.Received(1).ClearTenantAsync(tenantId, Arg.Any<CancellationToken>());
    }
}
