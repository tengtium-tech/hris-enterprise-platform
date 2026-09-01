namespace Hris.Foundation.Identity.Domain;

/// <summary>
/// The Infrastructure-layer boundary <see cref="MfaFactor"/>'s own remarks call for --
/// producing the opaque <c>SecretReference</c> a newly enrolled factor stores, per
/// identity-framework.md's Multi-Factor Authentication section. Declared here for the
/// same reason <see cref="IPasswordHasher"/> is: generating and safeguarding the actual
/// cryptographic factor (a TOTP seed, a hardware key registration, an OTP delivery
/// address) is Infrastructure, never Domain (`CTR-ARC-001`); this port only asks for a
/// reference by which Infrastructure can find that material again.
///
/// <see cref="ProvisionAsync"/> is asynchronous even though this Sprint's
/// implementation is not, matching <c>ILogSink.WriteAsync</c>'s own reasoning: a real
/// provider integration (an authenticator-app enrollment ceremony, an SMS/Email OTP
/// send, a hardware key registration round-trip) is genuinely asynchronous, and this
/// contract should not need to change shape once one is wired in.
/// </summary>
public interface IMfaSecretProvisioner
{
    Task<string> ProvisionAsync(MfaFactorType factorType, CancellationToken cancellationToken);
}
