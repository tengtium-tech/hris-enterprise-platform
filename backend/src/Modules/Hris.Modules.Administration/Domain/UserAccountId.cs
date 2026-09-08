using Hris.SharedKernel;

namespace Hris.Modules.Administration.Domain;

/// <summary>
/// Identity of the <see cref="UserAccount"/> Aggregate Root. Source:
/// docs/04-modules/administration/domain/entities.md's UserAccount section.
/// </summary>
public readonly record struct UserAccountId(Guid Value) : IStronglyTypedId;
