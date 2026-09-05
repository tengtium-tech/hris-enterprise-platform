using System.Net;
using FluentAssertions;
using Xunit;

namespace Hris.Api.Tests;

/// <summary>
/// api-standards.md's own Rate Limiting section, exercised end to end against the
/// real host: OperationsEndpoints applies the <c>lookup-by-identifier</c> policy
/// (100 requests per minute, per RateLimitingServiceCollectionExtensions), so this
/// class's own requests share none of the other test classes' rate-limit state --
/// each test class gets its own <see cref="HrisApiFactory"/> instance and therefore
/// its own limiter, the identical "one real database (and here, one limiter) per
/// test class" isolation <c>Hris.Infrastructure.IntegrationTests</c> already
/// establishes.
/// </summary>
public sealed class RateLimitingTests : IClassFixture<HrisApiFactory>
{
    private readonly HttpClient _client;

    public RateLimitingTests(HrisApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ExceedingTheLimit_Returns429_WithTheDocumentedHeaders_AndAStableCode()
    {
        var operationId = Guid.NewGuid();
        HttpResponseMessage? rejected = null;

        // The lookup-by-identifier policy permits 100 requests per window; the
        // 101st in the same window is rejected.
        for (var i = 0; i < 101 && rejected is null; i++)
        {
            var response = await _client.GetAsync(new Uri($"/api/v1/operations/{operationId}", UriKind.Relative));
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                rejected = response;
            }
        }

        rejected.Should().NotBeNull("the 101st request within one window must be rejected");
        rejected!.Headers.GetValues("RateLimit-Limit").Should().ContainSingle().Which.Should().Be("100");
        rejected.Headers.GetValues("RateLimit-Remaining").Should().ContainSingle().Which.Should().Be("0");
        rejected.Headers.GetValues("RateLimit-Reset").Should().ContainSingle();
        rejected.Headers.GetValues("Retry-After").Should().ContainSingle();
    }
}
