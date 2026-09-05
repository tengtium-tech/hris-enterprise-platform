using FluentAssertions;
using Hris.Foundation.Caching.Domain;
using Xunit;

namespace Hris.Foundation.Caching.Tests.Domain;

public sealed class CacheEntryOptionsTests
{
    [Fact]
    public void NeverExpire_HasNoAbsoluteOrSlidingExpiration()
    {
        CacheEntryOptions.NeverExpire.AbsoluteExpiration.Should().BeNull();
        CacheEntryOptions.NeverExpire.SlidingExpiration.Should().BeNull();
    }

    [Fact]
    public void Constructor_CarriesTheGivenAbsoluteExpiration()
    {
        var options = new CacheEntryOptions(AbsoluteExpiration: TimeSpan.FromMinutes(5));

        options.AbsoluteExpiration.Should().Be(TimeSpan.FromMinutes(5));
        options.SlidingExpiration.Should().BeNull();
    }

    [Fact]
    public void Constructor_CarriesTheGivenSlidingExpiration()
    {
        var options = new CacheEntryOptions(SlidingExpiration: TimeSpan.FromMinutes(2));

        options.SlidingExpiration.Should().Be(TimeSpan.FromMinutes(2));
        options.AbsoluteExpiration.Should().BeNull();
    }
}
