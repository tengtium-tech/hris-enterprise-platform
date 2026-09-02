using FluentAssertions;
using Hris.Foundation.Tenant.Domain;
using Xunit;

namespace Hris.Foundation.Tenant.Tests.Domain;

public sealed class TenantCodeTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Fails_WhenValueIsNullOrWhitespace(string? value)
    {
        var result = TenantCode.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TenantErrors.TenantCodeRequired);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("-abc")]
    [InlineData("abc-")]
    [InlineData("ab_c")]
    [InlineData("ABC.def")]
    public void Create_Fails_WhenValueIsNotAValidSubdomainLabel(string value)
    {
        var result = TenantCode.Create(value);

        result.IsFailure.Should().BeTrue("a tenant code resolves tenant context via a subdomain, per tenant-framework.md");
        result.Error.Should().Be(TenantErrors.TenantCodeInvalidFormat);
    }

    [Fact]
    public void Create_Fails_WhenValueExceeds63Characters()
    {
        var tooLong = new string('a', 64);

        var result = TenantCode.Create(tooLong);

        result.IsFailure.Should().BeTrue("RFC 1035 DNS labels cap at 63 characters");
        result.Error.Should().Be(TenantErrors.TenantCodeInvalidFormat);
    }

    [Fact]
    public void Create_Succeeds_AtExactly3Characters()
    {
        var result = TenantCode.Create("abc");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_Succeeds_AtExactly63Characters()
    {
        var result = TenantCode.Create(new string('a', 63));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_Succeeds_WithInternalHyphens()
    {
        var result = TenantCode.Create("acme-manufacturing");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("acme-manufacturing");
    }

    [Fact]
    public void Create_TrimsAndLowercases()
    {
        var result = TenantCode.Create("  ACME-Corp  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("acme-corp");
    }

    [Fact]
    public void Equality_IsByValue()
    {
        var first = TenantCode.Create("acme-corp").Value;
        var second = TenantCode.Create("ACME-CORP").Value;

        first.Should().Be(second, "TenantCode normalizes case, so the same code differing only by case is the same value");
    }

    [Fact]
    public void Equality_Differs_ForDifferentCodes()
    {
        var first = TenantCode.Create("acme-corp").Value;
        var second = TenantCode.Create("other-corp").Value;

        first.Should().NotBe(second);
    }
}
