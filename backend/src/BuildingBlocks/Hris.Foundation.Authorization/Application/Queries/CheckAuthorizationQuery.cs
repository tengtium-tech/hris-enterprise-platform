using Hris.Application.Abstractions;
using Hris.Foundation.Authorization.Application.Dtos;
using Hris.Foundation.Authorization.Application.Mapping;
using Hris.Foundation.Authorization.Domain;
using Hris.Foundation.Identity.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Authorization.Application.Queries;

/// <summary>
/// The single evaluation point authorization-framework.md's own Centralized
/// Evaluation section and ADR-0002 require: every module calls this, never a role-
/// name comparison of its own (`CTR-AUT-001`). A thin wrapper over
/// <see cref="AuthorizationEvaluator.EvaluateAsync"/> -- see that Domain Service's own
/// remarks for exactly which of the document's eight Permission Evaluation steps it
/// covers in this Sprint.
///
/// Deliberately a Query, not a Command, even though
/// <see cref="AuthorizationDecision"/> carries an <see cref="Hris.SharedKernel.IDomainEvent"/>
/// for a caller to publish (see that type's own remarks): publishing it here, on
/// every evaluation, would make every authorization check a transactional write --
/// directly working against this framework's own stated NFR ("Support millions of
/// authorization evaluations daily... minimal latency") and ADR-0002's own listed
/// Negative consequence ("requires caching to avoid becoming a performance
/// bottleneck"). ADR-0002's own Consequential Rules also only call for "Authorization
/// decisions on *sensitive resources*" to be auditable, not every decision
/// unconditionally -- selecting which resources qualify is Audit Framework's own
/// concern, and Audit Framework does not have an Infrastructure layer yet (it is
/// built after this one in Sprint 3's bootstrap order). Publishing this decision's own
/// event through Event Framework is deferred to that framework's own build, the same
/// "wired in incrementally as siblings come online" resolution IMPLEMENTATION-PLAN.md's
/// own dependency-cycle finding already applies elsewhere in this Sprint -- not
/// invented here ahead of a consumer that can act on it.
/// </summary>
public sealed record CheckAuthorizationQuery(
    Guid PrincipalId,
    string ResourceType,
    PermissionAction Action,
    OrganizationalScopeLevel ScopeLevel,
    Guid ScopeId) : IQuery<Result<AuthorizationDecisionDto>>;

internal sealed class CheckAuthorizationQueryHandler : IRequestHandler<CheckAuthorizationQuery, Result<AuthorizationDecisionDto>>
{
    private readonly AuthorizationEvaluator _evaluator;
    private readonly TimeProvider _timeProvider;

    public CheckAuthorizationQueryHandler(AuthorizationEvaluator evaluator, TimeProvider timeProvider)
    {
        _evaluator = Guard.AgainstNull(evaluator, nameof(evaluator));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<AuthorizationDecisionDto>> Handle(CheckAuthorizationQuery request, CancellationToken cancellationToken)
    {
        var permissionResult = PermissionKey.Create(request.ResourceType, request.Action);
        if (permissionResult.IsFailure)
        {
            return Result.Failure<AuthorizationDecisionDto>(permissionResult.Error);
        }

        var scopeResult = OrganizationalScope.Create(request.ScopeLevel, request.ScopeId);
        if (scopeResult.IsFailure)
        {
            return Result.Failure<AuthorizationDecisionDto>(scopeResult.Error);
        }

        var decision = await _evaluator.EvaluateAsync(
            new UserAccountId(request.PrincipalId),
            permissionResult.Value,
            scopeResult.Value,
            _timeProvider.GetUtcNow(),
            cancellationToken).ConfigureAwait(false);

        return Result.Success(decision.ToDto());
    }
}
