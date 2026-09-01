using Hris.Application.Abstractions;
using Hris.Foundation.Identity.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Identity.Application.Commands;

/// <summary>
/// One of identity-framework.md's five Client-Facing Commands: "Current password, new
/// password, actor (the caller)." <see cref="ActorUserAccountId"/> is that actor,
/// supplied by the caller rather than resolved from an ambient "current user" service
/// -- Identity Framework's own Infrastructure layer has no such accessor yet (see
/// <c>LoggingService</c>'s own remarks on the identical gap); a future API-layer
/// endpoint populates this from the authenticated request's own session once that
/// accessor exists, not invented here as a placeholder.
///
/// Scoped unconditionally to the caller's own account per identity-framework.md's
/// "Both Are Scoped to the Caller's Own Identity, Always" -- there is deliberately no
/// target-user-id parameter here; resetting a *different* user's password belongs to
/// `../04-modules/administration/`'s own Account Commands instead.
/// </summary>
public sealed record ChangeOwnPasswordCommand(
    Guid ActorUserAccountId,
    Guid TenantId,
    string CurrentPassword,
    string NewPassword) : ICommand<Result>;

internal sealed class ChangeOwnPasswordCommandHandler : IRequestHandler<ChangeOwnPasswordCommand, Result>
{
    private readonly IUserAccountRepository _repository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly TimeProvider _timeProvider;

    public ChangeOwnPasswordCommandHandler(
        IUserAccountRepository repository,
        IPasswordHasher passwordHasher,
        TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _passwordHasher = Guard.AgainstNull(passwordHasher, nameof(passwordHasher));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ChangeOwnPasswordCommand request, CancellationToken cancellationToken)
    {
        var account = await _repository
            .GetByIdAsync(new UserAccountId(request.ActorUserAccountId), request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (account is null)
        {
            return Result.Failure(IdentityErrors.AccountNotFound);
        }

        if (account.PasswordHash is null || !_passwordHasher.Verify(request.CurrentPassword, account.PasswordHash))
        {
            return Result.Failure(IdentityErrors.CurrentPasswordIncorrect);
        }

        var hashResult = _passwordHasher.Hash(request.NewPassword);
        if (hashResult.IsFailure)
        {
            return Result.Failure(hashResult.Error);
        }

        return account.ChangePassword(hashResult.Value, _timeProvider.GetUtcNow());
    }
}
