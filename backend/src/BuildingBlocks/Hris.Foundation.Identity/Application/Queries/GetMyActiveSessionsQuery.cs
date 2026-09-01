using Hris.Application.Abstractions;
using Hris.Foundation.Identity.Application.Dtos;
using Hris.Foundation.Identity.Application.Mapping;
using Hris.Foundation.Identity.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Identity.Application.Queries;

/// <summary>
/// One of identity-framework.md's five Client-Facing Commands and Queries: "The
/// caller's own active sessions -- device/client, approximate location, last-active
/// timestamp." "Active" is evaluated as of query time via <see cref="Session.IsActive"/>
/// (revoked and expired sessions are both excluded) -- a revoked session is exactly
/// what <c>RevokeMySessionCommand</c> already let the caller act on, so surfacing it
/// again here would only clutter the "sessions you could still revoke" list this query
/// exists to answer.
/// </summary>
public sealed record GetMyActiveSessionsQuery(Guid UserAccountId, Guid TenantId) : IQuery<Result<IReadOnlyList<SessionDto>>>;

internal sealed class GetMyActiveSessionsQueryHandler
    : IRequestHandler<GetMyActiveSessionsQuery, Result<IReadOnlyList<SessionDto>>>
{
    private readonly IUserAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public GetMyActiveSessionsQueryHandler(IUserAccountRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<IReadOnlyList<SessionDto>>> Handle(
        GetMyActiveSessionsQuery request, CancellationToken cancellationToken)
    {
        var account = await _repository
            .GetByIdAsync(new UserAccountId(request.UserAccountId), request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (account is null)
        {
            return Result.Failure<IReadOnlyList<SessionDto>>(IdentityErrors.AccountNotFound);
        }

        var nowUtc = _timeProvider.GetUtcNow();

        IReadOnlyList<SessionDto> sessions = account.Sessions
            .Where(session => session.IsActive(nowUtc))
            .Select(session => session.ToDto())
            .ToList();

        return Result.Success(sessions);
    }
}
