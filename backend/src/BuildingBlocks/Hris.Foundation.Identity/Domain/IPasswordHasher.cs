using Hris.SharedKernel;

namespace Hris.Foundation.Identity.Domain;

/// <summary>
/// The Infrastructure-layer boundary <see cref="PasswordHash"/>'s own remarks call
/// for -- "an <c>IPasswordHasher</c>-shaped Infrastructure service" that computes and
/// verifies the adaptive hash digest, per identity-framework.md's Security
/// Considerations ("Store credentials using an industry-standard adaptive hashing
/// algorithm") and `CTR-ARC-001` (cryptography is Infrastructure, never Domain).
/// Declared here, in Domain, for the same reason <see cref="IUserAccountRepository"/>
/// is -- repositories.md's "interface in the Domain layer... implementation in
/// Infrastructure" split applies to any port a Domain-adjacent concept depends on, not
/// only to persistence.
///
/// <see cref="Verify"/> takes the already-constructed <see cref="PasswordHash"/> rather
/// than a raw string, so a caller can never accidentally compare a plaintext value
/// against another plaintext value -- the type system forces the digest through
/// <see cref="PasswordHash.Create"/> first.
/// </summary>
public interface IPasswordHasher
{
    Result<PasswordHash> Hash(string plainTextPassword);

    bool Verify(string plainTextPassword, PasswordHash hash);
}
