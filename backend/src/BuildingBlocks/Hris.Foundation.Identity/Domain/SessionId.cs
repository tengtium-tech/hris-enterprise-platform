using Hris.SharedKernel;

namespace Hris.Foundation.Identity.Domain;

/// <summary>
/// Identity of a <see cref="Session"/> child Entity, unique within its owning
/// <see cref="UserAccount"/>.
/// </summary>
public readonly record struct SessionId(Guid Value) : IStronglyTypedId;
