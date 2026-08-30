using Hris.SharedKernel;

namespace Hris.Foundation.Identity.Domain;

/// <summary>
/// Identity of an <see cref="MfaFactor"/> child Entity, unique within its owning
/// <see cref="UserAccount"/>.
/// </summary>
public readonly record struct MfaFactorId(Guid Value) : IStronglyTypedId;
