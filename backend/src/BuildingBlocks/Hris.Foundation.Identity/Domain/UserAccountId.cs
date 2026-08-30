using Hris.SharedKernel;

namespace Hris.Foundation.Identity.Domain;

/// <summary>
/// Identity of the <see cref="UserAccount"/> Aggregate Root. Source:
/// docs/03-foundation/identity-framework.md.
/// </summary>
public readonly record struct UserAccountId(Guid Value) : IStronglyTypedId;
