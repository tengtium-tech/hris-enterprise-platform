using Hris.Application.Abstractions;
using Hris.Foundation.Entitlement.Application.Dtos;
using Hris.Foundation.Entitlement.Application.Mapping;
using Hris.Foundation.Entitlement.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Entitlement.Application.Queries;

/// <summary>
/// The single evaluation point entitlement-framework.md's own Entitlement Evaluation
/// section requires: every module calls this before authorization, never its own
/// edition-composition lookup (the same "one evaluation point, never a check of its
/// own" discipline <c>CheckAuthorizationQuery</c>'s own remarks already establish for
/// permission checks).
///
/// Deliberately a Query, not a Command, mirroring <c>CheckAuthorizationQuery</c>'s own
/// reasoning: <see cref="EntitlementDecision"/> carries no event to publish (this
/// framework's own Domain Events section states why), so there is no transactional
/// write to avoid here either -- the shape is simply consistent with that sibling
/// evaluation point, not driven by the same performance concern that shape was chosen
/// for there.
/// </summary>
public sealed record EvaluateEntitlementQuery(
    TenantEditionCode Edition,
    ProcessPackCode Pack,
    MaturityLevel RequiredMaturityLevel) : IQuery<Result<EntitlementDecisionDto>>;

internal sealed class EvaluateEntitlementQueryHandler : IRequestHandler<EvaluateEntitlementQuery, Result<EntitlementDecisionDto>>
{
    public Task<Result<EntitlementDecisionDto>> Handle(EvaluateEntitlementQuery request, CancellationToken cancellationToken)
    {
        var decision = EntitlementEvaluator.Evaluate(request.Edition, request.Pack, request.RequiredMaturityLevel);

        return Task.FromResult(Result.Success(decision.ToDto()));
    }
}
