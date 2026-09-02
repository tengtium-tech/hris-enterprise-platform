using Hris.SharedKernel;

namespace Hris.Foundation.Extension.Domain;

/// <summary>
/// Identity of the <see cref="ExtensionPoint"/> Aggregate Root. Source:
/// docs/03-foundation/extension-framework.md.
/// </summary>
public readonly record struct ExtensionPointId(Guid Value) : IStronglyTypedId;
