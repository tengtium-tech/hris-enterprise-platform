using FluentAssertions;
using Hris.Foundation.Entitlement.Application.Queries;
using Hris.Foundation.Entitlement.Application.Validators;
using Hris.Foundation.Entitlement.Domain;
using Xunit;

namespace Hris.Foundation.Entitlement.Tests.Application;

/// <summary>
/// One valid-passes/invalid-fails pair per validator, the same scope
/// <see cref="Hris.Foundation.Authorization.Tests.Application.AuthorizationCommandValidatorsTests"/>
/// already establishes for its own framework -- confirming each enum parameter is
/// actually wired to <c>IsInEnum()</c>, not re-testing FluentValidation's own
/// mechanics.
/// </summary>
public sealed class EntitlementQueryValidatorsTests
{
    [Fact]
    public void EvaluateEntitlementQueryValidator_AcceptsAValidQuery_AndRejectsAnUndefinedEdition()
    {
        var validator = new EvaluateEntitlementQueryValidator();
        var valid = new EvaluateEntitlementQuery(TenantEditionCode.Growth, ProcessPackCode.Payroll, MaturityLevel.Standard);
        var invalid = valid with { Edition = (TenantEditionCode)999 };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void EvaluateEntitlementQueryValidator_RejectsAnUndefinedPack()
    {
        var validator = new EvaluateEntitlementQueryValidator();
        var invalid = new EvaluateEntitlementQuery(TenantEditionCode.Growth, (ProcessPackCode)999, MaturityLevel.Standard);

        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void EvaluateEntitlementQueryValidator_RejectsAnUndefinedRequiredMaturityLevel()
    {
        var validator = new EvaluateEntitlementQueryValidator();
        var invalid = new EvaluateEntitlementQuery(TenantEditionCode.Growth, ProcessPackCode.Payroll, (MaturityLevel)999);

        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetEditionEntitlementSummaryQueryValidator_AcceptsAValidQuery_AndRejectsAnUndefinedEdition()
    {
        var validator = new GetEditionEntitlementSummaryQueryValidator();
        var valid = new GetEditionEntitlementSummaryQuery(TenantEditionCode.Enterprise);
        var invalid = valid with { Edition = (TenantEditionCode)999 };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }
}
