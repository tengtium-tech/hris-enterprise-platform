using Hris.Application.Abstractions;
using Hris.Foundation.Identity.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Identity.Application.Commands;

/// <summary>
/// Provisions a new <see cref="UserAccount"/>, per this framework's own Scope
/// ("User Identity... Account Lifecycle"). Not one of identity-framework.md's five
/// Client-Facing Commands -- provisioning is never self-service -- but it is also not
/// administration-module territory: that module's own future Account Commands
/// (`../04-modules/administration/README.md`) call this command rather than
/// constructing a <see cref="UserAccount"/> directly, the same layered relationship
/// <c>LoggingService</c> has with <c>ResolveConfigurationValueQuery</c>. Without some
/// primitive that can ever call <see cref="UserAccount.Create"/>, no account -- and so
/// no login, no session, no MFA enrollment -- could ever exist; this is that primitive.
///
/// <see cref="InitialPassword"/> is optional and only meaningful for
/// <see cref="AuthenticationProvider.LocalKey"/> accounts -- a federated account
/// (Entra ID, SAML, etc.) never holds a local credential at all. When supplied, the
/// handler hashes it and calls <see cref="UserAccount.ChangePassword"/> immediately
/// after <see cref="UserAccount.Create"/>, which that method's own guard explicitly
/// allows while the account is still <see cref="UserAccountStatus.Invited"/> -- this is
/// the only way a Local account's very first credential is ever set, since
/// <c>ChangeOwnPasswordCommand</c> requires an existing password to check against.
/// </summary>
public sealed record CreateUserAccountCommand(
    Guid TenantId,
    string Username,
    string EmailAddress,
    string? DisplayName,
    IdentityType IdentityType,
    string? AuthenticationProviderKey,
    string? InitialPassword,
    Guid? LinkedIdentityId) : ICommand<Result<Guid>>;

internal sealed class CreateUserAccountCommandHandler : IRequestHandler<CreateUserAccountCommand, Result<Guid>>
{
    private readonly IUserAccountRepository _repository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly TimeProvider _timeProvider;

    public CreateUserAccountCommandHandler(
        IUserAccountRepository repository,
        IPasswordHasher passwordHasher,
        TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _passwordHasher = Guard.AgainstNull(passwordHasher, nameof(passwordHasher));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(CreateUserAccountCommand request, CancellationToken cancellationToken)
    {
        var usernameResult = Username.Create(request.Username);
        if (usernameResult.IsFailure)
        {
            return Result.Failure<Guid>(usernameResult.Error);
        }

        var emailResult = EmailAddress.Create(request.EmailAddress);
        if (emailResult.IsFailure)
        {
            return Result.Failure<Guid>(emailResult.Error);
        }

        var providerResult = string.IsNullOrWhiteSpace(request.AuthenticationProviderKey)
            ? Result.Success(AuthenticationProvider.Local())
            : AuthenticationProvider.Create(request.AuthenticationProviderKey);
        if (providerResult.IsFailure)
        {
            return Result.Failure<Guid>(providerResult.Error);
        }

        // identity-framework.md's Identity Principles: "Centralized Identity" -- a
        // username is the login handle within a tenant, so two accounts sharing one
        // would make login resolution ambiguous, the same reasoning
        // CreateConfigurationSettingCommandHandler's own key+scope uniqueness check
        // documents for Configuration Framework.
        if (await _repository.ExistsAsync(usernameResult.Value, request.TenantId, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure<Guid>(IdentityErrors.UsernameAlreadyExists);
        }

        var nowUtc = _timeProvider.GetUtcNow();

        var accountResult = UserAccount.Create(
            request.TenantId,
            usernameResult.Value,
            emailResult.Value,
            request.DisplayName,
            request.IdentityType,
            providerResult.Value,
            nowUtc,
            request.LinkedIdentityId);

        if (accountResult.IsFailure)
        {
            return Result.Failure<Guid>(accountResult.Error);
        }

        var account = accountResult.Value;

        if (!string.IsNullOrWhiteSpace(request.InitialPassword))
        {
            var hashResult = _passwordHasher.Hash(request.InitialPassword);
            if (hashResult.IsFailure)
            {
                return Result.Failure<Guid>(hashResult.Error);
            }

            var changeResult = account.ChangePassword(hashResult.Value, nowUtc);
            if (changeResult.IsFailure)
            {
                return Result.Failure<Guid>(changeResult.Error);
            }
        }

        await _repository.AddAsync(account, cancellationToken).ConfigureAwait(false);

        return Result.Success(account.Id.Value);
    }
}
