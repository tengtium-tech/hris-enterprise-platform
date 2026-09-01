using Hris.Application.Abstractions;
using Hris.Foundation.Identity.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Identity.Application.Commands;

/// <summary>
/// The seven <see cref="UserAccountStatus"/> transitions identity-framework.md's
/// Identity Lifecycle section names, beyond provisioning itself
/// (<see cref="CreateUserAccountCommand"/>): Activate, Lock, Unlock, Suspend,
/// Reinstate, Disable, Archive. None of these seven is one of the five
/// Client-Facing Commands and Queries -- identity-framework.md's own "Both Are Scoped
/// to the Caller's Own Identity" section reserves that self-service surface for
/// <c>ChangeOwnPasswordCommand</c>/<c>EnrollMfaFactorCommand</c>/<c>RemoveMfaFactorCommand</c>/
/// <c>RevokeMySessionCommand</c> alone. These seven are the underlying primitives a
/// future administration-module Account Command calls (per that document's own
/// "provisioning, suspending, deprovisioning... remains administration's own Account
/// Commands" read together with Module Isolation's "Modules communicate through
/// Contracts and Events") -- nothing outside this framework may call
/// <see cref="UserAccount"/>'s own transition methods directly, since
/// <see cref="IUserAccountRepository"/> is this framework's own port.
///
/// Grouped into one file for the identical reason
/// <see cref="Configuration.Application.Commands.ConfigurationVersionLifecycleCommands"/>
/// states for its own five: each command/handler pair is a mechanical "load the
/// Aggregate by id, call the one method that already enforces the transition's
/// invariants, translate the Result" wrapper.
/// </summary>
public sealed record ActivateUserAccountCommand(Guid UserAccountId, Guid TenantId) : ICommand<Result>;

public sealed record LockUserAccountCommand(Guid UserAccountId, Guid TenantId) : ICommand<Result>;

public sealed record UnlockUserAccountCommand(Guid UserAccountId, Guid TenantId) : ICommand<Result>;

public sealed record SuspendUserAccountCommand(Guid UserAccountId, Guid TenantId) : ICommand<Result>;

public sealed record ReinstateUserAccountCommand(Guid UserAccountId, Guid TenantId) : ICommand<Result>;

public sealed record DisableUserAccountCommand(Guid UserAccountId, Guid TenantId) : ICommand<Result>;

public sealed record ArchiveUserAccountCommand(Guid UserAccountId, Guid TenantId) : ICommand<Result>;

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
        var account = await _repository
            .GetByIdAsync(new UserAccountId(request.UserAccountId), request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return account is null
            ? Result.Failure(IdentityErrors.AccountNotFound)
            : account.Activate(_timeProvider.GetUtcNow());
    }
}

internal sealed class LockUserAccountCommandHandler : IRequestHandler<LockUserAccountCommand, Result>
{
    private readonly IUserAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public LockUserAccountCommandHandler(IUserAccountRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(LockUserAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _repository
            .GetByIdAsync(new UserAccountId(request.UserAccountId), request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return account is null
            ? Result.Failure(IdentityErrors.AccountNotFound)
            : account.Lock(_timeProvider.GetUtcNow());
    }
}

internal sealed class UnlockUserAccountCommandHandler : IRequestHandler<UnlockUserAccountCommand, Result>
{
    private readonly IUserAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public UnlockUserAccountCommandHandler(IUserAccountRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(UnlockUserAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _repository
            .GetByIdAsync(new UserAccountId(request.UserAccountId), request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return account is null
            ? Result.Failure(IdentityErrors.AccountNotFound)
            : account.Unlock(_timeProvider.GetUtcNow());
    }
}

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
        var account = await _repository
            .GetByIdAsync(new UserAccountId(request.UserAccountId), request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return account is null
            ? Result.Failure(IdentityErrors.AccountNotFound)
            : account.Suspend(_timeProvider.GetUtcNow());
    }
}

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
        var account = await _repository
            .GetByIdAsync(new UserAccountId(request.UserAccountId), request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return account is null
            ? Result.Failure(IdentityErrors.AccountNotFound)
            : account.Reinstate(_timeProvider.GetUtcNow());
    }
}

internal sealed class DisableUserAccountCommandHandler : IRequestHandler<DisableUserAccountCommand, Result>
{
    private readonly IUserAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public DisableUserAccountCommandHandler(IUserAccountRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(DisableUserAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _repository
            .GetByIdAsync(new UserAccountId(request.UserAccountId), request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return account is null
            ? Result.Failure(IdentityErrors.AccountNotFound)
            : account.Disable(_timeProvider.GetUtcNow());
    }
}

internal sealed class ArchiveUserAccountCommandHandler : IRequestHandler<ArchiveUserAccountCommand, Result>
{
    private readonly IUserAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ArchiveUserAccountCommandHandler(IUserAccountRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ArchiveUserAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _repository
            .GetByIdAsync(new UserAccountId(request.UserAccountId), request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return account is null
            ? Result.Failure(IdentityErrors.AccountNotFound)
            : account.Archive(_timeProvider.GetUtcNow());
    }
}
