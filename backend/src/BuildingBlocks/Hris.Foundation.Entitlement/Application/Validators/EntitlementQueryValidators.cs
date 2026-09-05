using FluentValidation;
using Hris.Foundation.Entitlement.Application.Queries;

namespace Hris.Foundation.Entitlement.Application.Validators;

/// <summary>
/// application-pipeline.md's Validation Behavior scope: "Required fields... Business-
/// independent validation." Every parameter these two queries take is a closed
/// enumeration, so the only business-independent check available is that the caller
/// passed a defined member -- the same <c>IsInEnum()</c> defensive check
/// <c>AuditQueryValidators</c> already applies to its own enum parameters.
/// <c>ListProcessPacksQuery</c> has no validator: it takes no parameters.
/// </summary>
public sealed class EvaluateEntitlementQueryValidator : AbstractValidator<EvaluateEntitlementQuery>
{
    public EvaluateEntitlementQueryValidator()
    {
        RuleFor(q => q.Edition).IsInEnum();
        RuleFor(q => q.Pack).IsInEnum();
        RuleFor(q => q.RequiredMaturityLevel).IsInEnum();
    }
}

public sealed class GetEditionEntitlementSummaryQueryValidator : AbstractValidator<GetEditionEntitlementSummaryQuery>
{
    public GetEditionEntitlementSummaryQueryValidator()
    {
        RuleFor(q => q.Edition).IsInEnum();
    }
}
