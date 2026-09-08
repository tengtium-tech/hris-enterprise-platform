using Hris.Application.Abstractions;
using Hris.Modules.Administration.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Administration.Application.Commands;

/// <summary>
/// Records <see cref="ActingAdministrator"/> and <see cref="DelegatorUserAccountId"/>
/// separately, per commands.md's own reasoning: they differ when an administrator
/// creates a delegation on behalf of an absent manager. <see cref="ActingAdministrator"/>
/// is accepted for audit purposes at the command boundary but is not itself a
/// field the <see cref="AdministrativeDelegation"/> Aggregate stores -- the
/// Aggregate's own state answers "whose authority" (<c>DelegatorUserAccountId</c>)
/// and "who exercises it" (<c>DelegateUserAccountId</c>); "who set this up" is an
/// audit-log concern outside this Aggregate's own boundary.
/// </summary>
public sealed record CreateDelegationCommand(
    Guid TenantId,
    Guid DelegatorUserAccountId,
    Guid DelegateUserAccountId,
    IReadOnlyList<DelegatedAuthorityItem> DelegatedAuthority,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string? Reason,
    Guid? ApprovalReference,
    Guid ActingAdministrator) : ICommand<Result<Guid>>;

internal sealed class CreateDelegationCommandHandler : IRequestHandler<CreateDelegationCommand, Result<Guid>>
{
    private readonly IAdministrativeDelegationRepository _delegationRepository;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly TimeProvider _timeProvider;

    public CreateDelegationCommandHandler(
        IAdministrativeDelegationRepository delegationRepository, IUserAccountRepository userAccountRepository,
        TimeProvider timeProvider)
    {
        _delegationRepository = Guard.AgainstNull(delegationRepository, nameof(delegationRepository));
        _userAccountRepository = Guard.AgainstNull(userAccountRepository, nameof(userAccountRepository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(CreateDelegationCommand request, CancellationToken cancellationToken)
    {
        var nowUtc = _timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(nowUtc.UtcDateTime);

        var delegator = await _userAccountRepository.GetByIdAsync(new UserAccountId(request.DelegatorUserAccountId), cancellationToken)
            .ConfigureAwait(false);
        var delegate_ = await _userAccountRepository.GetByIdAsync(new UserAccountId(request.DelegateUserAccountId), cancellationToken)
            .ConfigureAwait(false);

        var authorityExceedsDelegatorHoldings = delegator is null || request.DelegatedAuthority.Any(
            item => !delegator.HoldsRoleAtOrBroaderThan(
                RoleReference.ForCanonical(item.Role), OrganizationalScope.Create(item.ScopeLevel, item.ScopeTargetId).Value, today));

        var delegateHeldRoles = delegate_?.RoleAssignments
            .Where(a => a.RevokedOn is null && a.IsEffectiveOn(today) && a.Role.Kind == RoleKind.Canonical)
            .Select(a => a.Role.CanonicalRole!.Value) ?? [];
        var combinedRoles = delegateHeldRoles.Concat(request.DelegatedAuthority.Select(item => item.Role));
        var violatesSeparationOfDuties = Domain.UserAccount.ViolatesSeparationOfDuties(combinedRoles);

        var id = new DelegationId(Guid.NewGuid());
        var delegationResult = AdministrativeDelegation.Create(
            id, request.TenantId, request.DelegatorUserAccountId, request.DelegateUserAccountId, request.DelegatedAuthority,
            request.PeriodStart, request.PeriodEnd, request.Reason, request.ApprovalReference, authorityExceedsDelegatorHoldings,
            violatesSeparationOfDuties, nowUtc);
        if (delegationResult.IsFailure)
        {
            return Result.Failure<Guid>(delegationResult.Error);
        }

        await _delegationRepository.AddAsync(delegationResult.Value, cancellationToken).ConfigureAwait(false);
        return Result.Success(delegationResult.Value.Id.Value);
    }
}

/// <summary>
/// Scheduled operation (delegated-administration.md's own "Scheduled Confers
/// Nothing"); exposed as a real command so the capability exists and is tested
/// even though the scheduled trigger itself is deferred this Sprint.
/// </summary>
public sealed record ActivateDelegationCommand(Guid DelegationId, Guid TenantId) : ICommand<Result>;

internal sealed class ActivateDelegationCommandHandler : IRequestHandler<ActivateDelegationCommand, Result>
{
    private readonly IAdministrativeDelegationRepository _delegationRepository;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly TimeProvider _timeProvider;

    public ActivateDelegationCommandHandler(
        IAdministrativeDelegationRepository delegationRepository, IUserAccountRepository userAccountRepository,
        TimeProvider timeProvider)
    {
        _delegationRepository = Guard.AgainstNull(delegationRepository, nameof(delegationRepository));
        _userAccountRepository = Guard.AgainstNull(userAccountRepository, nameof(userAccountRepository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ActivateDelegationCommand request, CancellationToken cancellationToken)
    {
        var result = await AdministrationLookup.LoadDelegationForTenantAsync(
            _delegationRepository, request.DelegationId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return Result.Failure(result.Error);
        }

        var nowUtc = _timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(nowUtc.UtcDateTime);
        var delegation = result.Value;

        var delegator = await _userAccountRepository.GetByIdAsync(new UserAccountId(delegation.DelegatorUserAccountId), cancellationToken)
            .ConfigureAwait(false);
        var delegate_ = await _userAccountRepository.GetByIdAsync(new UserAccountId(delegation.DelegateUserAccountId), cancellationToken)
            .ConfigureAwait(false);

        var authorityExceedsDelegatorHoldings = delegator is null || delegation.DelegatedAuthority.Any(
            item => !delegator.HoldsRoleAtOrBroaderThan(
                RoleReference.ForCanonical(item.Role), OrganizationalScope.Create(item.ScopeLevel, item.ScopeTargetId).Value, today));

        var delegateHeldRoles = delegate_?.RoleAssignments
            .Where(a => a.RevokedOn is null && a.IsEffectiveOn(today) && a.Role.Kind == RoleKind.Canonical)
            .Select(a => a.Role.CanonicalRole!.Value) ?? [];
        var combinedRoles = delegateHeldRoles.Concat(delegation.DelegatedAuthority.Select(item => item.Role));
        var violatesSeparationOfDuties = Domain.UserAccount.ViolatesSeparationOfDuties(combinedRoles);

        return delegation.Activate(authorityExceedsDelegatorHoldings, violatesSeparationOfDuties, nowUtc);
    }
}

/// <summary>Scheduled operation, no actor (AR-043); exposed as a real command for the same reason as <see cref="ActivateDelegationCommand"/>.</summary>
public sealed record ExpireDelegationCommand(Guid DelegationId, Guid TenantId) : ICommand<Result>;

internal sealed class ExpireDelegationCommandHandler : IRequestHandler<ExpireDelegationCommand, Result>
{
    private readonly IAdministrativeDelegationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ExpireDelegationCommandHandler(IAdministrativeDelegationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ExpireDelegationCommand request, CancellationToken cancellationToken)
    {
        var result = await AdministrationLookup.LoadDelegationForTenantAsync(
            _repository, request.DelegationId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return result.IsFailure ? Result.Failure(result.Error) : result.Value.Expire(_timeProvider.GetUtcNow());
    }
}

public sealed record RevokeDelegationCommand(Guid DelegationId, Guid TenantId, Guid RevokedBy, string? Reason) : ICommand<Result>;

internal sealed class RevokeDelegationCommandHandler : IRequestHandler<RevokeDelegationCommand, Result>
{
    private readonly IAdministrativeDelegationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RevokeDelegationCommandHandler(IAdministrativeDelegationRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RevokeDelegationCommand request, CancellationToken cancellationToken)
    {
        var result = await AdministrationLookup.LoadDelegationForTenantAsync(
            _repository, request.DelegationId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return result.IsFailure
            ? Result.Failure(result.Error)
            : result.Value.Revoke(request.RevokedBy, request.Reason, _timeProvider.GetUtcNow());
    }
}
