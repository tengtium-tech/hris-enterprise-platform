using Hris.SharedKernel;

namespace Hris.Foundation.Configuration.Domain;

/// <summary>
/// Identity of a <see cref="ConfigurationVersion"/> child Entity, unique within the
/// context of its owning <see cref="ConfigurationSetting"/> Aggregate, per
/// docs/02-architecture/04-domain-driven-design/strongly-typed-ids.md's Entity
/// Identity section.
/// </summary>
public readonly record struct ConfigurationVersionId(Guid Value) : IStronglyTypedId;
