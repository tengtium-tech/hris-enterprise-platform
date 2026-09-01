using Hris.Application.Abstractions;
using Hris.Foundation.Identity.Application.Dtos;
using Hris.Foundation.Identity.Application.Mapping;
using Hris.Foundation.Identity.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Identity.Application.Queries;

/// <summary>
/// One of identity-framework.md's five Client-Facing Commands and Queries: "The
/// caller's own account details -- username, email address, display name, status,
/// authentication provider, MFA status, last login." Scoped unconditionally to the
/// caller per that document's "Both Are Scoped to the Caller's Own Identity" -- there
/// is deliberately no target-user-id parameter; reading a *different* user's account
/// belongs to `../04-modules/administration/`'s own screens instead.
/// </summary>
public sealed record GetMyAccountQuery(Guid UserAccountId, Guid TenantId) : IQuery<Result<UserAccountDto>>;

internal sealed class GetMyAccountQueryHandler : IRequestHandler<GetMyAccountQuery, Result<UserAccountDto>>
{
    private readonly IUserAccountRepository _repository;

    public GetMyAccountQueryHandler(IUserAccountRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<UserAccountDto>> Handle(GetMyAccountQuery request, CancellationToken cancellationToken)
    {
        var account = await _repository
            .GetByIdAsync(new UserAccountId(request.UserAccountId), request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return account is null
            ? Result.Failure<UserAccountDto>(IdentityErrors.AccountNotFound)
            : Result.Success(account.ToDto());
    }
}
