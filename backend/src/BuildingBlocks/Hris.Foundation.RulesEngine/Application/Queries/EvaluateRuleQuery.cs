using Hris.Application.Abstractions;
using Hris.Foundation.Identity.Domain;
using Hris.Foundation.RulesEngine.Application.Dtos;
using Hris.Foundation.RulesEngine.Application.Mapping;
using Hris.Foundation.RulesEngine.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.RulesEngine.Application.Queries;

/// <summary>
/// Evaluates one rule's Active version against a caller-supplied set of facts -- a
/// thin wrapper over the existing <see cref="RuleEvaluator"/> domain service.
///
/// Deliberately a Query, not a Command, even though <see cref="RuleEvaluationResult"/>
/// carries a domain event for a caller to publish (per that type's own remarks): the
/// identical reasoning <c>CheckAuthorizationQuery</c>'s own remarks give for the same
/// choice applies here with equal force -- <see cref="RuleEvaluator.EvaluateAsync"/>
/// performs no persistence of its own (it only reads), so publishing an event on every
/// evaluation would be a *new* database write on a path this framework's own NFRs
/// state must "support millions of rule evaluations daily" with "minimal latency."
/// rules-engine.md's own Implementation Guidance also only calls for auditing "the
/// decisions rules produce on sensitive resources," not every evaluation
/// unconditionally -- selecting which rules qualify is a caller/Audit Framework
/// concern, not something this query forces on every invocation.
/// </summary>
public sealed record EvaluateRuleQuery(
    Guid RuleDefinitionId,
    IReadOnlyDictionary<string, string> Facts,
    Guid? InitiatedByPrincipalId) : IQuery<Result<RuleEvaluationResultDto>>;

internal sealed class EvaluateRuleQueryHandler : IRequestHandler<EvaluateRuleQuery, Result<RuleEvaluationResultDto>>
{
    private readonly RuleEvaluator _evaluator;
    private readonly TimeProvider _timeProvider;

    public EvaluateRuleQueryHandler(RuleEvaluator evaluator, TimeProvider timeProvider)
    {
        _evaluator = Guard.AgainstNull(evaluator, nameof(evaluator));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<RuleEvaluationResultDto>> Handle(EvaluateRuleQuery request, CancellationToken cancellationToken)
    {
        var context = RuleEvaluationContext.Create(request.Facts);

        var result = await _evaluator.EvaluateAsync(
            new RuleDefinitionId(request.RuleDefinitionId),
            context,
            request.InitiatedByPrincipalId.HasValue ? new UserAccountId(request.InitiatedByPrincipalId.Value) : null,
            _timeProvider.GetUtcNow(),
            cancellationToken).ConfigureAwait(false);

        return result.IsFailure
            ? Result.Failure<RuleEvaluationResultDto>(result.Error)
            : Result.Success(result.Value.ToDto());
    }
}
