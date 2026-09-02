using Hris.SharedKernel;

namespace Hris.Foundation.Extension.Domain;

/// <summary>
/// Identity of the <see cref="Hook"/> Aggregate Root. Source:
/// docs/03-foundation/extension-framework.md's own Hook section.
/// </summary>
public readonly record struct HookId(Guid Value) : IStronglyTypedId;
