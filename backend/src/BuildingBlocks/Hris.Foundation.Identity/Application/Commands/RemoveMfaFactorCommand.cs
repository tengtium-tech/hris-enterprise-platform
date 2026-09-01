using Hris.Application.Abstractions;
using Hris.Foundation.Identity.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Identity.Application.Commands;

/// <summary>
/// One of identity-framework.md's five Client-Facing Commands: "Factor identifier,
/// actor." Scoped to the caller's own account the same way every command in this file
/// group is: <see cref="UserAccount.RemoveMfaFactor"/> only ever searches the loaded
/// account's own <c>MfaFactors</c> collection, so a factor id belonging to a different
/// account resolves to <see cref="IdentityErrors.MfaFactorNotFound"/>, never a
/// cross-account mutation.
/// </summary>
public sealed record RemoveMfaFactorCommand(
    Guid ActorUserAccountId,
    Guid TenantId,
    Guid MfaFactorId) : ICommand<Result>;

internal sealed class RemoveMfaFactorCommandHandler : IRequestHandler<RemoveMfaFactorCommand, Result>
{
    private readonly IUserAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RemoveMfaFactorCommandHandler(IUserAccountRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RemoveMfaFactorCommand request, CancellationToken cancellationToken)
    {
        var account = await _repository
            .GetByIdAsync(new UserAccountId(request.ActorUserAccountId), request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return account is null
            ? Result.Failure(IdentityErrors.AccountNotFound)
            : account.RemoveMfaFactor(new MfaFactorId(request.MfaFactorId), _timeProvider.GetUtcNow());
    }
}
