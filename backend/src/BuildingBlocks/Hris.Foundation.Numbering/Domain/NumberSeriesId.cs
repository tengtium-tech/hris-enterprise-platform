using Hris.SharedKernel;

namespace Hris.Foundation.Numbering.Domain;

/// <summary>
/// Identity of the <see cref="NumberSeries"/> Aggregate Root. Source:
/// docs/03-foundation/numbering-framework.md, Core Concepts ("A Number Series defines
/// how identifiers are generated").
/// </summary>
public readonly record struct NumberSeriesId(Guid Value) : IStronglyTypedId;
