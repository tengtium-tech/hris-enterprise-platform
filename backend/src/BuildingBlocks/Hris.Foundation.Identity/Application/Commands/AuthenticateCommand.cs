using Hris.Application.Abstractions;
using Hris.Foundation.Configuration.Application.Queries;
using Hris.Foundation.Configuration.Domain;
using Hris.Foundation.Identity.Application.Dtos;
using Hris.Foundation.Identity.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Identity.Application.Commands;

/// <summary>
/// Verifies a username/password pair and, on success, opens a <see cref="Session"/> --
/// identity-framework.md's Purpose #1, "Provide secure authentication." Not one of the
/// five Client-Facing Commands (there is no caller identity yet to scope this to); this
/// is the "internal framework operation" that document's own Client-Facing section
/// says everything above it describes.
///
/// Returns <see cref="Result{TValue}"/>-success with <see cref="AuthenticationResultDto.IsAuthenticated"/>
/// <c>false</c> for every credential-rejection reason -- unknown username, wrong
/// password, non-Active account -- rather than a <see cref="Result"/> failure, per
/// identity-framework.md's own "Never confirm whether an account exists in a failed
/// authentication response; account enumeration is a disclosure": a distinguishable
/// failure code per rejection reason would itself be the disclosure this line
/// prohibits. This also matters structurally: <see cref="Behaviors.TransactionBehavior{TRequest,TResponse}"/>
/// only calls <c>SaveChangesAsync</c> when the response is not a failed
/// <see cref="Result"/>; a wrong-password attempt still needs
/// <see cref="UserAccount.RecordFailedAuthentication"/>'s increment persisted so
/// brute-force lockout (`NFR-SE`, Security Considerations' "Brute Force Protection")
/// actually accumulates across attempts. Making the command itself succeed even when
/// the login attempt does not is what lets that mutation reach the database.
///
/// <see cref="Result.Failure"/> is reserved here for the one case that is not a
/// rejected login at all: <see cref="UserAccount.RecordSuccessfulAuthenticationAndCreateSession"/>
/// failing with <see cref="IdentityErrors.TooManyActiveSessions"/> after credentials
/// already verified correct -- no enumeration risk remains once a password has already
/// matched, so that specific, distinguishable failure is safe to surface.
/// </summary>
public sealed record AuthenticateCommand(
    Guid TenantId,
    string Username,
    string Password,
    string DeviceLabel,
    string? ApproximateLocation) : ICommand<Result<AuthenticationResultDto>>;

internal sealed class AuthenticateCommandHandler : IRequestHandler<AuthenticateCommand, Result<AuthenticationResultDto>>
{
    /// <summary>
    /// Configuration Framework keys this handler resolves at Global scope, mirroring
    /// <c>LoggingService.MinimumSeverityConfigurationKey</c>'s own pattern for
    /// consuming identity-framework.md's stated "Upstream Dependencies: Configuration
    /// Framework" line. Not Tenant-scoped: session/lockout policy defaults are a
    /// platform-wide security posture in this Sprint, the same simplification
    /// <c>LoggingService</c> makes for its own threshold; a per-tenant override is a
    /// straightforward future extension of the same resolution call, not a shape
    /// change.
    /// </summary>
    internal const string SessionLifetimeMinutesConfigurationKey = "Identity.SessionLifetimeMinutes";
    internal const string MaxConcurrentSessionsConfigurationKey = "Identity.MaxConcurrentSessions";
    internal const string MaxFailedAuthenticationAttemptsConfigurationKey = "Identity.MaxFailedAuthenticationAttempts";

    private const int _defaultSessionLifetimeMinutes = 480;
    private const int _defaultMaxConcurrentSessions = 5;
    private const int _defaultMaxFailedAuthenticationAttempts = 5;

    private static readonly AuthenticationResultDto _rejected = new(false, null, null, null);

    private readonly IUserAccountRepository _repository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISender _sender;
    private readonly TimeProvider _timeProvider;

    public AuthenticateCommandHandler(
        IUserAccountRepository repository,
        IPasswordHasher passwordHasher,
        ISender sender,
        TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _passwordHasher = Guard.AgainstNull(passwordHasher, nameof(passwordHasher));
        _sender = Guard.AgainstNull(sender, nameof(sender));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<AuthenticationResultDto>> Handle(AuthenticateCommand request, CancellationToken cancellationToken)
    {
        var usernameResult = Username.Create(request.Username);
        if (usernameResult.IsFailure)
        {
            return Result.Success(_rejected);
        }

        var account = await _repository
            .GetByUsernameAsync(usernameResult.Value, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        var nowUtc = _timeProvider.GetUtcNow();

        if (account is null)
        {
            return Result.Success(_rejected);
        }

        if (account.Status != UserAccountStatus.Active || account.PasswordHash is null
            || !_passwordHasher.Verify(request.Password, account.PasswordHash))
        {
            // A non-Active account (Locked, Suspended, ...) never reaches
            // RecordFailedAuthentication -- that method only mutates state for an
            // Active account (see its own guard), so calling it here would do nothing
            // but risk masking a genuine AccountNotActive Result this handler must
            // still not leak.
            if (account.Status == UserAccountStatus.Active)
            {
                account.RecordFailedAuthentication(await ResolveMaxFailedAttemptsAsync(nowUtc, cancellationToken).ConfigureAwait(false), nowUtc);
            }

            return Result.Success(_rejected);
        }

        var sessionLifetimeMinutes = await ResolveIntSettingAsync(
            SessionLifetimeMinutesConfigurationKey, _defaultSessionLifetimeMinutes, nowUtc, cancellationToken).ConfigureAwait(false);
        var maxConcurrentSessions = await ResolveIntSettingAsync(
            MaxConcurrentSessionsConfigurationKey, _defaultMaxConcurrentSessions, nowUtc, cancellationToken).ConfigureAwait(false);

        var sessionResult = account.RecordSuccessfulAuthenticationAndCreateSession(
            request.TenantId,
            request.DeviceLabel,
            request.ApproximateLocation,
            nowUtc,
            TimeSpan.FromMinutes(sessionLifetimeMinutes),
            maxConcurrentSessions);

        if (sessionResult.IsFailure)
        {
            return Result.Failure<AuthenticationResultDto>(sessionResult.Error);
        }

        return Result.Success(new AuthenticationResultDto(
            true,
            account.Id.Value,
            sessionResult.Value.Id.Value,
            sessionResult.Value.ExpiresAtUtc));
    }

    private async Task<int> ResolveMaxFailedAttemptsAsync(DateTimeOffset asOfUtc, CancellationToken cancellationToken) =>
        await ResolveIntSettingAsync(
            MaxFailedAuthenticationAttemptsConfigurationKey, _defaultMaxFailedAuthenticationAttempts, asOfUtc, cancellationToken)
            .ConfigureAwait(false);

    private async Task<int> ResolveIntSettingAsync(
        string key, int defaultValue, DateTimeOffset asOfUtc, CancellationToken cancellationToken)
    {
        var query = new ResolveConfigurationValueQuery(key, [ConfigurationScope.Global()], DateOnly.FromDateTime(asOfUtc.UtcDateTime));
        var result = await _sender.Send(query, cancellationToken).ConfigureAwait(false);

        // Absence of an operator-set override resolves to the default rather than
        // propagating ConfigurationErrors.VersionNotFound -- the same reasoning
        // LoggingService.ResolveMinimumSeverityAsync documents for its own threshold:
        // authentication must remain available even when no policy override has ever
        // been configured.
        return result.IsSuccess && int.TryParse(result.Value, out var parsed) && parsed > 0
            ? parsed
            : defaultValue;
    }
}
