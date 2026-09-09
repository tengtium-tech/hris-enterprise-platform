using Hris.Application.Abstractions;
using Hris.Modules.Workflow.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Workflow.Application.Commands;

/// <summary>
/// Creates a bounded transfer of approval authority. Source:
/// docs/04-modules/workflow/application/commands.md's own Delegation Commands table.
///
/// <see cref="ActingUser"/> and <see cref="DelegatorUserAccountId"/> are recorded
/// separately, as that document requires, because they differ when an administrator
/// creates coverage on behalf of an absent manager.
///
/// WR-042 is computed here rather than supplied by the caller: whether the
/// delegator holds the authority only by delegation is answerable from this
/// module's own delegations, by asking whether an active delegation names them as
/// delegate. WR-041 remains caller-supplied, since a delegator's own approval
/// standing depends on administration's role assignments this module cannot read.
/// </summary>
public sealed record CreateApprovalDelegationCommand(
    Guid TenantId,
    Guid DelegatorUserAccountId,
    Guid DelegateUserAccountId,
    IReadOnlyList<Guid>? Scope,
    bool CoversAllProcesses,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string? Reason,
    Guid? ApprovalReference,
    bool ScopeExceedsDelegatorStanding,
    Guid ActingUser) : ICommand<Result<Guid>>;

internal sealed class CreateApprovalDelegationCommandHandler
    : IRequestHandler<CreateApprovalDelegationCommand, Result<Guid>>
{
    private readonly IApprovalDelegationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CreateApprovalDelegationCommandHandler(IApprovalDelegationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(CreateApprovalDelegationCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var nowUtc = _timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(nowUtc.UtcDateTime);

        var delegationsToDelegator = await _repository
            .ListByDelegateAsync(request.TenantId, request.DelegatorUserAccountId, cancellationToken).ConfigureAwait(false);

        var authorityIsItselfDelegated = delegationsToDelegator.Any(existing =>
            existing.Status == ApprovalDelegationStatus.Active
            && existing.Period.Contains(today)
            && (request.CoversAllProcesses
                || existing.CoversAllProcesses
                || (request.Scope ?? []).Any(process => existing.Scope.Contains(process))));

        var result = ApprovalDelegation.Create(
            new ApprovalDelegationId(Guid.NewGuid()), request.TenantId, request.DelegatorUserAccountId,
            request.DelegateUserAccountId, request.Scope, request.CoversAllProcesses, request.PeriodStart,
            request.PeriodEnd, request.Reason, request.ApprovalReference, request.ScopeExceedsDelegatorStanding,
            authorityIsItselfDelegated, request.ActingUser, nowUtc);

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _repository.AddAsync(result.Value, cancellationToken).ConfigureAwait(false);
        return Result.Success(result.Value.Id.Value);
    }
}

/// <summary>
/// Scheduled operation, not a user command in the ordinary sense: a delegation
/// begins because its period began. WR-041 is re-validated here because weeks may
/// pass between scheduling and activation and the delegator's own standing may have
/// changed in the interim.
/// </summary>
public sealed record ActivateApprovalDelegationCommand(
    Guid TenantId, Guid DelegationId, bool ScopeExceedsDelegatorStanding) : ICommand<Result>;

internal sealed class ActivateApprovalDelegationCommandHandler
    : IRequestHandler<ActivateApprovalDelegationCommand, Result>
{
    private readonly IApprovalDelegationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ActivateApprovalDelegationCommandHandler(IApprovalDelegationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ActivateApprovalDelegationCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var delegationResult = await WorkflowLookup
            .LoadDelegationForTenantAsync(_repository, request.DelegationId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return delegationResult.IsFailure
            ? Result.Failure(delegationResult.Error)
            : delegationResult.Value.Activate(request.ScopeExceedsDelegatorStanding, _timeProvider.GetUtcNow());
    }
}

/// <summary>Automatic at period end, carrying no actor because none acted (WR-043).</summary>
public sealed record ExpireApprovalDelegationCommand(Guid TenantId, Guid DelegationId) : ICommand<Result>;

internal sealed class ExpireApprovalDelegationCommandHandler : IRequestHandler<ExpireApprovalDelegationCommand, Result>
{
    private readonly IApprovalDelegationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ExpireApprovalDelegationCommandHandler(IApprovalDelegationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ExpireApprovalDelegationCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var delegationResult = await WorkflowLookup
            .LoadDelegationForTenantAsync(_repository, request.DelegationId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return delegationResult.IsFailure
            ? Result.Failure(delegationResult.Error)
            : delegationResult.Value.Expire(_timeProvider.GetUtcNow());
    }
}

/// <summary>
/// Ends a delegation before its scheduled expiry. Idempotent by convergence. There
/// is deliberately no delete command: deletion would destroy attribution for
/// approvals already performed under the delegation.
/// </summary>
public sealed record RevokeApprovalDelegationCommand(
    Guid TenantId, Guid DelegationId, Guid ActingUser, string? Reason) : ICommand<Result>;

internal sealed class RevokeApprovalDelegationCommandHandler : IRequestHandler<RevokeApprovalDelegationCommand, Result>
{
    private readonly IApprovalDelegationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RevokeApprovalDelegationCommandHandler(IApprovalDelegationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RevokeApprovalDelegationCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var delegationResult = await WorkflowLookup
            .LoadDelegationForTenantAsync(_repository, request.DelegationId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return delegationResult.IsFailure
            ? Result.Failure(delegationResult.Error)
            : delegationResult.Value.Revoke(request.ActingUser, request.Reason, _timeProvider.GetUtcNow());
    }
}
