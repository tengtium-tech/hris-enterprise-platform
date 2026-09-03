using Hris.SharedKernel;

namespace Hris.Foundation.Numbering.Domain;

/// <summary>
/// Identity of the <see cref="IssuedNumber"/> Aggregate Root. Source:
/// docs/03-foundation/numbering-framework.md, Number Lifecycle. A separate Aggregate
/// Root from <see cref="NumberSeries"/>, not a child Entity of it -- the same
/// population-scale reason Extension Framework's own <c>Hook</c> is kept independent of
/// <c>ExtensionPoint</c>: a series' own Non-Functional Requirements state "Support
/// millions of generated identifiers," and loading a series to read its own
/// configuration must never risk pulling millions of issued-number rows along with it.
/// </summary>
public readonly record struct IssuedNumberId(Guid Value) : IStronglyTypedId;
