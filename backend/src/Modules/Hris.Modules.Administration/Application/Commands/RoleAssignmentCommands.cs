using Hris.Application.Abstractions;
using Hris.Modules.Administration.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Administration.Application.Commands;

/// <summary>
/// Grants a role at a scope. Rejected per role-assignments.md's own "The Three
/// Rules" -- AR-001 (self-grant), AR-002 (escalation beyond the granter's own
/// authority), AR-003 (separation of duties against the target's complete
/// assignment set) -- all enforced inside <see cref="Domain.UserAccount.GrantRole"/>
/// itself, never in this handler.
/// </summary>
public sealed record GrantRoleCommand(
    Guid UserAccountId,
    Guid TenantId,
    RoleKind RoleKind,
    CanonicalRole? CanonicalRole,
    Guid? TenantRoleId,
    ScopeLevel ScopeLevel,
    Guid? ScopeTargetId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    Guid GrantedBy,
    string? Reason,
    Guid? ApprovalReference) : ICommand<Result<Guid>>;

internal sealed class GrantRoleCommandHandler : IRequestHandler<GrantRoleCommand, Result<Guid>>
{
    private readonly IUserAccountRepository _repository;
    private readonly ITenantRoleRepository _tenantRoleRepository;
    private readonly TimeProvider _timeProvider;

    public GrantRoleCommandHandler(IUserAccountRepository repository, ITenantRoleRepository tenantRoleRepository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _tenantRoleRepository = Guard.AgainstNull(tenantRoleRepository, nameof(tenantRoleRepository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(GrantRoleCommand request, CancellationToken cancellationToken)
    {
        var targetResult = await AdministrationLookup.LoadUserAccountForTenantAsync(
            _repository, request.UserAccountId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (targetResult.IsFailure)
        {
            return Result.Failure<Guid>(targetResult.Error);
        }

        var roleResult = await ResolveRoleReferenceAsync(request, cancellationToken).ConfigureAwait(false);
        if (roleResult.IsFailure)
        {
            return Result.Failure<Guid>(roleResult.Error);
        }

        var scopeResult = OrganizationalScope.Create(request.ScopeLevel, request.ScopeTargetId);
        if (scopeResult.IsFailure)
        {
            return Result.Failure<Guid>(scopeResult.Error);
        }

        var granter = await _repository.GetByIdAsync(new UserAccountId(request.GrantedBy), cancellationToken).ConfigureAwait(false);
        var granterHasSufficientAuthority = granter is not null
            && granter.HoldsRoleAtOrBroaderThan(roleResult.Value, scopeResult.Value, request.EffectiveFrom);

        return targetResult.Value.GrantRole(
            roleResult.Value, scopeResult.Value, request.EffectiveFrom, request.EffectiveTo, request.GrantedBy, request.Reason,
            request.ApprovalReference, granterHasSufficientAuthority, _timeProvider.GetUtcNow());
    }

    private async Task<Result<RoleReference>> ResolveRoleReferenceAsync(GrantRoleCommand request, CancellationToken cancellationToken)
    {
        if (request.RoleKind == RoleKind.Canonical)
        {
            return request.CanonicalRole is null
                ? Result.Failure<RoleReference>(AdministrationErrors.TenantRoleReferenceRequiresPublishedRole)
                : Result.Success(RoleReference.ForCanonical(request.CanonicalRole.Value));
        }

        if (request.TenantRoleId is null)
        {
            return Result.Failure<RoleReference>(AdministrationErrors.TenantRoleReferenceRequiresPublishedRole);
        }

        var tenantRole = await _tenantRoleRepository.GetByIdAsync(new TenantRoleId(request.TenantRoleId.Value), cancellationToken)
            .ConfigureAwait(false);
        var isPublished = tenantRole is not null && tenantRole.TenantId == request.TenantId && tenantRole.Status == TenantRoleStatus.Published;

        return RoleReference.ForTenantRole(request.TenantRoleId.Value, tenantRole?.Name, isPublished);
    }
}

public sealed record RevokeRoleCommand(Guid UserAccountId, Guid TenantId, Guid RoleAssignmentId, Guid RevokedBy, string? Reason)
    : ICommand<Result>;

internal sealed class RevokeRoleCommandHandler : IRequestHandler<RevokeRoleCommand, Result>
{
    private readonly IUserAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RevokeRoleCommandHandler(IUserAccountRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RevokeRoleCommand request, CancellationToken cancellationToken)
    {
        var targetResult = await AdministrationLookup.LoadUserAccountForTenantAsync(
            _repository, request.UserAccountId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (targetResult.IsFailure)
        {
            return Result.Failure(targetResult.Error);
        }

        var otherAdministrators = await _repository
            .CountOtherActiveTenantAdministratorsAsync(request.TenantId, request.UserAccountId, cancellationToken)
            .ConfigureAwait(false);

        return targetResult.Value.RevokeRole(
            request.RoleAssignmentId, request.RevokedBy, request.Reason, otherAdministrators == 0, _timeProvider.GetUtcNow());
    }
}

/// <summary>
/// Scheduled operation (role-assignments.md's own "Automatic Expiry"); exposed as
/// a real command so the capability exists and is tested even though the
/// scheduled trigger itself is deferred this Sprint (see
/// STATUS.md's own "Known open items"), matching the precedent Employee's own
/// deferred Lifecycle-progression commands already established.
/// </summary>
public sealed record ExpireRoleAssignmentCommand(Guid UserAccountId, Guid TenantId, Guid RoleAssignmentId) : ICommand<Result>;

internal sealed class ExpireRoleAssignmentCommandHandler : IRequestHandler<ExpireRoleAssignmentCommand, Result>
{
    private readonly IUserAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ExpireRoleAssignmentCommandHandler(IUserAccountRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ExpireRoleAssignmentCommand request, CancellationToken cancellationToken)
    {
        var result = await AdministrationLookup.LoadUserAccountForTenantAsync(
            _repository, request.UserAccountId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return result.IsFailure
            ? Result.Failure(result.Error)
            : result.Value.ExpireAssignment(request.RoleAssignmentId, _timeProvider.GetUtcNow());
    }
}
