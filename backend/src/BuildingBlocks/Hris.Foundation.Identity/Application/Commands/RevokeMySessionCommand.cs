using Hris.Application.Abstractions;
using Hris.Foundation.Identity.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Identity.Application.Commands;

/// <summary>
/// One of identity-framework.md's five Client-Facing Commands: "Session identifier,
/// actor." Idempotent by construction -- <see cref="UserAccount.RevokeSession"/>'s own
/// remarks state a retry against an already-revoked session returns the same
/// successful outcome, per that document's own "`RevokeMySessionCommand` Never Revokes
/// the Calling Session Silently Mid-Request" section -- this handler adds nothing on
/// top of that; the idempotence lives entirely in the Aggregate.
/// </summary>
public sealed record RevokeMySessionCommand(
    Guid ActorUserAccountId,
    Guid TenantId,
    Guid SessionId) : ICommand<Result>;

internal sealed class RevokeMySessionCommandHandler : IRequestHandler<RevokeMySessionCommand, Result>
{
    private readonly IUserAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RevokeMySessionCommandHandler(IUserAccountRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RevokeMySessionCommand request, CancellationToken cancellationToken)
    {
        var account = await _repository
            .GetByIdAsync(new UserAccountId(request.ActorUserAccountId), request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return account is null
            ? Result.Failure(IdentityErrors.AccountNotFound)
            : account.RevokeSession(new SessionId(request.SessionId), _timeProvider.GetUtcNow());
    }
}
