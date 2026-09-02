using FluentAssertions;
using Hris.Foundation.Authorization.Domain;
using Xunit;

namespace Hris.Foundation.Authorization.Tests.Domain;

public sealed class OrganizationalScopeTests
{
    [Fact]
    public void Create_Succeeds_WithANonEmptyScopeId()
    {
        var scopeId = Guid.NewGuid();

        var result = OrganizationalScope.Create(OrganizationalScopeLevel.Tenant, scopeId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Level.Should().Be(OrganizationalScopeLevel.Tenant);
        result.Value.ScopeId.Should().Be(scopeId);
    }

    [Theory]
    [InlineData(OrganizationalScopeLevel.Tenant)]
    [InlineData(OrganizationalScopeLevel.Company)]
    [InlineData(OrganizationalScopeLevel.LegalEntity)]
    [InlineData(OrganizationalScopeLevel.BusinessUnit)]
    [InlineData(OrganizationalScopeLevel.Department)]
    public void Create_Fails_WhenScopeIdIsEmpty_RegardlessOfLevel(OrganizationalScopeLevel level)
    {
        var result = OrganizationalScope.Create(level, Guid.Empty);

        result.IsFailure.Should().BeTrue("every level requires a concrete scope id; there is no Global level here to exempt");
        result.Error.Should().Be(AuthorizationErrors.ScopeIdRequired);
    }

    [Fact]
    public void Covers_ReturnsTrue_WhenSameLevelAndSameScopeId()
    {
        var scopeId = Guid.NewGuid();
        var grantScope = OrganizationalScope.Create(OrganizationalScopeLevel.Department, scopeId).Value;
        var resourceScope = OrganizationalScope.Create(OrganizationalScopeLevel.Department, scopeId).Value;

        grantScope.Covers(resourceScope).Should().BeTrue();
    }

    [Fact]
    public void Covers_ReturnsFalse_WhenLevelsDiffer()
    {
        var scopeId = Guid.NewGuid();
        var grantScope = OrganizationalScope.Create(OrganizationalScopeLevel.Department, scopeId).Value;
        var resourceScope = OrganizationalScope.Create(OrganizationalScopeLevel.BusinessUnit, scopeId).Value;

        grantScope.Covers(resourceScope).Should().BeFalse("a grant does not automatically cover a narrower or broader level it was not assigned at");
    }

    [Fact]
    public void Covers_ReturnsFalse_WhenScopeIdsDiffer_AtTheSameLevel()
    {
        var grantScope = OrganizationalScope.Create(OrganizationalScopeLevel.Department, Guid.NewGuid()).Value;
        var resourceScope = OrganizationalScope.Create(OrganizationalScopeLevel.Department, Guid.NewGuid()).Value;

        grantScope.Covers(resourceScope).Should().BeFalse("CTR-AUT-006: scope is enforced, not only assigned");
    }

    [Fact]
    public void Covers_Throws_WhenResourceScopeIsNull()
    {
        var grantScope = TestData.Scope();

        var act = () => grantScope.Covers(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Equality_IsByValue_NotReference()
    {
        var scopeId = Guid.NewGuid();
        var first = OrganizationalScope.Create(OrganizationalScopeLevel.Tenant, scopeId).Value;
        var second = OrganizationalScope.Create(OrganizationalScopeLevel.Tenant, scopeId).Value;

        first.Should().Be(second);
        (first == second).Should().BeTrue();
        first.Equals(second).Should().BeTrue();
    }

    [Fact]
    public void Equality_Differs_WhenScopeIdDiffers()
    {
        var first = OrganizationalScope.Create(OrganizationalScopeLevel.Tenant, Guid.NewGuid()).Value;
        var second = OrganizationalScope.Create(OrganizationalScopeLevel.Tenant, Guid.NewGuid()).Value;

        first.Should().NotBe(second);
    }

    [Fact]
    public void ToString_IncludesLevelAndScopeId()
    {
        var scopeId = Guid.NewGuid();
        var scope = OrganizationalScope.Create(OrganizationalScopeLevel.Tenant, scopeId).Value;

        scope.ToString().Should().Be($"Tenant:{scopeId}");
    }
}
