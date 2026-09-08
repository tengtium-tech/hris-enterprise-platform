using Hris.Application.Abstractions;
using Hris.Modules.Administration.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Administration.Application.Commands;

/// <summary>
/// Creates a new account in <see cref="UserAccountStatus.Pending"/>.
/// <see cref="InitialAssignments"/> are subject to the full grant rules
/// (AR-001/AR-002/AR-003), applied by the handler calling
/// <see cref="Domain.UserAccount.GrantRole"/> once per item immediately after
/// creation -- AR-019: "Provisioning is not a route around the grant rules."
/// </summary>
public sealed record ProvisionUserAccountCommand(
    Guid TenantId,
    AccountType AccountType,
    Guid? EmployeeId,
    DateOnly? ExpiryDate,
    Guid? ServiceAccountOwnerId,
    Guid ProvisionedBy,
    IReadOnlyList<InitialRoleAssignmentInput>? InitialAssignments) : ICommand<Result<Guid>>;

internal sealed class ProvisionUserAccountCommandHandler : IRequestHandler<ProvisionUserAccountCommand, Result<Guid>>
{
    private readonly IUserAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ProvisionUserAccountCommandHandler(IUserAccountRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(ProvisionUserAccountCommand request, CancellationToken cancellationToken)
    {
        var id = new UserAccountId(Guid.NewGuid());
        var nowUtc = _timeProvider.GetUtcNow();

        var accountResult = Domain.UserAccount.Create(
            id, request.TenantId, request.AccountType, request.EmployeeId, request.ExpiryDate, request.ServiceAccountOwnerId,
            request.ProvisionedBy, nowUtc);
        if (accountResult.IsFailure)
        {
            return Result.Failure<Guid>(accountResult.Error);
        }

        var account = accountResult.Value;

        if (request.InitialAssignments is { Count: > 0 })
        {
            var granter = await _repository.GetByIdAsync(new UserAccountId(request.ProvisionedBy), cancellationToken)
                .ConfigureAwait(false);
            var today = DateOnly.FromDateTime(nowUtc.UtcDateTime);

            foreach (var initial in request.InitialAssignments)
            {
                var role = RoleReference.ForCanonical(initial.Role);
                var scopeResult = OrganizationalScope.Create(initial.ScopeLevel, initial.ScopeTargetId);
                if (scopeResult.IsFailure)
                {
                    return Result.Failure<Guid>(scopeResult.Error);
                }

                var granterHasSufficientAuthority = granter is not null && granter.HoldsRoleAtOrBroaderThan(role, scopeResult.Value, today);

                var grantResult = account.GrantRole(
                    role, scopeResult.Value, today, null, request.ProvisionedBy, initial.Reason, null, granterHasSufficientAuthority,
                    nowUtc);
                if (grantResult.IsFailure)
                {
                    return Result.Failure<Guid>(grantResult.Error);
                }
            }
        }

        await _repository.AddAsync(account, cancellationToken).ConfigureAwait(false);
        return Result.Success(account.Id.Value);
    }
}

public sealed record ActivateUserAccountCommand(Guid UserAccountId, Guid TenantId) : ICommand<Result>;

internal sealed class ActivateUserAccountCommandHandler : IRequestHandler<ActivateUserAccountCommand, Result>
{
    private readonly IUserAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ActivateUserAccountCommandHandler(IUserAccountRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ActivateUserAccountCommand request, CancellationToken cancellationToken)
    {
        var result = await AdministrationLookup.LoadUserAccountForTenantAsync(
            _repository, request.UserAccountId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return result.IsFailure ? Result.Failure(result.Error) : result.Value.Activate(_timeProvider.GetUtcNow());
    }
}

public sealed record SuspendUserAccountCommand(Guid UserAccountId, Guid TenantId, string? Reason, Guid SuspendedBy) : ICommand<Result>;

internal sealed class SuspendUserAccountCommandHandler : IRequestHandler<SuspendUserAccountCommand, Result>
{
    private readonly IUserAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public SuspendUserAccountCommandHandler(IUserAccountRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(SuspendUserAccountCommand request, CancellationToken cancellationToken)
    {
        var result = await AdministrationLookup.LoadUserAccountForTenantAsync(
            _repository, request.UserAccountId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return Result.Failure(result.Error);
        }

        var otherAdministrators = await _repository
            .CountOtherActiveTenantAdministratorsAsync(request.TenantId, request.UserAccountId, cancellationToken)
            .ConfigureAwait(false);

        return result.Value.Suspend(request.Reason, request.SuspendedBy, otherAdministrators == 0, _timeProvider.GetUtcNow());
    }
}

public sealed record ReinstateUserAccountCommand(Guid UserAccountId, Guid TenantId) : ICommand<Result>;

internal sealed class ReinstateUserAccountCommandHandler : IRequestHandler<ReinstateUserAccountCommand, Result>
{
    private readonly IUserAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ReinstateUserAccountCommandHandler(IUserAccountRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ReinstateUserAccountCommand request, CancellationToken cancellationToken)
    {
        var result = await AdministrationLookup.LoadUserAccountForTenantAsync(
            _repository, request.UserAccountId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return result.IsFailure ? Result.Failure(result.Error) : result.Value.Reinstate(_timeProvider.GetUtcNow());
    }
}

public sealed record DeprovisionUserAccountCommand(
    Guid UserAccountId, Guid TenantId, string? Reason, Guid DeprovisionedBy) : ICommand<Result>;

internal sealed class DeprovisionUserAccountCommandHandler : IRequestHandler<DeprovisionUserAccountCommand, Result>
{
    private readonly IUserAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public DeprovisionUserAccountCommandHandler(IUserAccountRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(DeprovisionUserAccountCommand request, CancellationToken cancellationToken)
    {
        var result = await AdministrationLookup.LoadUserAccountForTenantAsync(
            _repository, request.UserAccountId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return Result.Failure(result.Error);
        }

        var otherAdministrators = await _repository
            .CountOtherActiveTenantAdministratorsAsync(request.TenantId, request.UserAccountId, cancellationToken)
            .ConfigureAwait(false);

        return result.Value.Deprovision(request.Reason, request.DeprovisionedBy, otherAdministrators == 0, _timeProvider.GetUtcNow());
    }
}
