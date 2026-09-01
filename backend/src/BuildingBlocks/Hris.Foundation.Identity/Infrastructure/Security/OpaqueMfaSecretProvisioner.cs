using System.Security.Cryptography;
using Hris.Foundation.Identity.Domain;

namespace Hris.Foundation.Identity.Infrastructure.Security;

/// <summary>
/// The Infrastructure-layer adapter <see cref="IMfaSecretProvisioner"/>'s own remarks
/// call for. Genuinely a placeholder, stated as one rather than left implicit: it
/// returns a cryptographically random opaque token, which satisfies
/// <see cref="MfaFactor.SecretReference"/>'s own documented shape ("opaque... never the
/// TOTP seed, hardware key public key material, or OTP delivery address itself") but
/// does not enroll a real authenticator-app secret, register a hardware key, or send a
/// verification OTP -- no TOTP library, WebAuthn ceremony, or SMS/Email OTP provider is
/// referenced anywhere in this solution yet, and technology-stack.md names none of
/// those specifically the way it names ASP.NET Core Identity for password hashing.
/// Replace this class, not <see cref="IMfaSecretProvisioner"/>'s own contract, once a
/// specific MFA provider is selected for each <see cref="MfaFactorType"/>.
/// </summary>
internal sealed class OpaqueMfaSecretProvisioner : IMfaSecretProvisioner
{
    public Task<string> ProvisionAsync(MfaFactorType factorType, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32)));
    }
}
