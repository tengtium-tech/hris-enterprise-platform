using FluentAssertions;
using Hris.Foundation.Entitlement.Domain;
using Hris.SharedKernel;
using Xunit;

namespace Hris.Foundation.Entitlement.Tests.Domain;

/// <summary>
/// Neither error is reached by this framework's own code -- <c>EvaluateEntitlementQuery</c>
/// always returns <c>Result.Success</c>, carrying the decision (Entitled or Denied) as
/// its own value, the same "the query succeeds at answering the question"
/// shape <c>CheckAuthorizationQuery</c> already establishes. These two errors exist
/// for a future caller (a business module's own command handler) to raise when it
/// turns an <see cref="EntitlementDecision"/> it received into its own command
/// failure -- this test confirms the catalog itself is shaped correctly (CTR-ENT-007's
/// own distinct category), not that this framework raises it.
/// </summary>
public sealed class EntitlementErrorsTests
{
    [Fact]
    public void PackNotActive_UsesTheEntitlementCategory_NeverAuthorization()
    {
        EntitlementErrors.PackNotActive.Category.Should().Be(ErrorCategory.Entitlement);
        EntitlementErrors.PackNotActive.Code.Should().Be("Entitlement.PackNotActive");
    }

    [Fact]
    public void MaturityLevelInsufficient_UsesTheEntitlementCategory_NeverAuthorization()
    {
        EntitlementErrors.MaturityLevelInsufficient.Category.Should().Be(ErrorCategory.Entitlement);
        EntitlementErrors.MaturityLevelInsufficient.Code.Should().Be("Entitlement.MaturityLevelInsufficient");
    }
}
