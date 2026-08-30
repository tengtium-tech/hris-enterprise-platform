using Hris.SharedKernel;

namespace Hris.Foundation.Identity.Domain;

/// <summary>
/// One enrolled multi-factor authentication factor, per identity-framework.md's
/// Multi-Factor Authentication section. A child Entity of <see cref="UserAccount"/>.
///
/// <see cref="SecretReference"/> is opaque -- an Infrastructure-layer secrets vault
/// key or provider-issued credential identifier, never the TOTP seed, hardware key
/// public key material, or OTP delivery address itself. Generating, storing, and
/// verifying the actual cryptographic factor is Infrastructure (`CTR-ARC-001`); the
/// Domain layer only tracks that a factor of a given type is enrolled and by
/// reference to what.
/// </summary>
public sealed class MfaFactor : Entity<MfaFactorId>
{
    public MfaFactorType FactorType { get; }

    public string SecretReference { get; }

    public DateTimeOffset EnrolledAtUtc { get; }

    public DateTimeOffset? RemovedAtUtc { get; private set; }

    internal MfaFactor(MfaFactorId id, MfaFactorType factorType, string secretReference, DateTimeOffset enrolledAtUtc)
        : base(id)
    {
        FactorType = factorType;
        SecretReference = secretReference;
        EnrolledAtUtc = enrolledAtUtc;
    }

    public bool IsActive => RemovedAtUtc is null;

    internal Result Remove(DateTimeOffset nowUtc)
    {
        if (RemovedAtUtc is not null)
        {
            return Result.Success();
        }

        RemovedAtUtc = nowUtc;
        return Result.Success();
    }
}
