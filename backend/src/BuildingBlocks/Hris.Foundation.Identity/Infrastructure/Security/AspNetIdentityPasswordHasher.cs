using Hris.Foundation.Identity.Domain;
using Hris.SharedKernel;
using Microsoft.AspNetCore.Identity;

namespace Hris.Foundation.Identity.Infrastructure.Security;

/// <summary>
/// The Infrastructure-layer adapter <see cref="IPasswordHasher"/>'s own remarks call
/// for, backed by <see cref="PasswordHasher{TUser}"/> -- technology-stack.md's Backend
/// table names "ASP.NET Core Identity" directly as this platform's own Password
/// Hashing standard, the identical "named directly, not an illustrative example"
/// status <c>SerilogLogSink</c>'s own remarks document for Serilog. This class
/// references that package (PBKDF2 with HMAC-SHA256, per its own default
/// implementation) specifically so <c>Hris.Foundation.Identity.Domain</c> stays free of
/// it, per Clean Architecture's inward dependency rule (`CTR-ARC-001`) -- this is the
/// one place that reference is allowed.
///
/// <see cref="PasswordHasher{TUser}"/> is generic over a "user" type purely for a
/// version-stamping extensibility hook its own default implementation does not use;
/// <see cref="UserAccount"/> is supplied only to satisfy that generic parameter, never
/// read by the hasher itself.
/// </summary>
internal sealed class AspNetIdentityPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<UserAccount> _hasher = new();

    public Result<PasswordHash> Hash(string plainTextPassword)
    {
        if (string.IsNullOrWhiteSpace(plainTextPassword))
        {
            return Result.Failure<PasswordHash>(IdentityErrors.PasswordHashRequired);
        }

        var hashed = _hasher.HashPassword(null!, plainTextPassword);
        return PasswordHash.Create(hashed);
    }

    public bool Verify(string plainTextPassword, PasswordHash hash)
    {
        Guard.AgainstNull(hash, nameof(hash));

        if (string.IsNullOrWhiteSpace(plainTextPassword))
        {
            return false;
        }

        var verificationResult = _hasher.VerifyHashedPassword(null!, hash.Value, plainTextPassword);
        return verificationResult is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
