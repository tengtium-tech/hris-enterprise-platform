using FluentAssertions;
using Xunit;

namespace Hris.Api.Tests;

/// <summary>
/// api-standards.md's own Error Response Format section: correlationId "ties the
/// error response to the structured log entry ... every request" carries. These
/// confirm the round trip CorrelationIdMiddleware itself establishes, independent
/// of any one endpoint's own business logic -- the health check endpoints already
/// wired in Program.cs are enough to exercise the middleware pipeline itself.
/// </summary>
public sealed class CorrelationIdTests : IClassFixture<HrisApiFactory>
{
    private readonly HttpClient _client;

    public CorrelationIdTests(HrisApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Response_CarriesAGeneratedCorrelationId_WhenTheRequestSuppliesNone()
    {
        var response = await _client.GetAsync(new Uri("/health/live", UriKind.Relative));

        response.Headers.TryGetValues("X-Correlation-Id", out var values).Should().BeTrue();
        values!.Should().ContainSingle().Which.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Response_EchoesTheCallerSuppliedCorrelationId_RatherThanGeneratingANewOne()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/health/live", UriKind.Relative));
        request.Headers.Add("X-Correlation-Id", "caller-supplied-id");

        var response = await _client.SendAsync(request);

        response.Headers.GetValues("X-Correlation-Id").Should().ContainSingle().Which.Should().Be("caller-supplied-id");
    }
}
