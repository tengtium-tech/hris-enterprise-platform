using Hris.SharedKernel;

namespace Hris.Foundation.Localization.Domain;

/// <summary>
/// Identity of the <see cref="CountryConfiguration"/> Aggregate Root. A synthetic
/// Guid rather than <see cref="CountryCode"/> itself: <see cref="AggregateRoot{TId}"/>
/// requires an <see cref="IStronglyTypedId"/>, which is Guid-backed throughout this
/// platform (strongly-typed-ids.md's own convention); <see cref="CountryConfiguration.Country"/>
/// remains the natural key a repository actually looks up by.
/// </summary>
public readonly record struct CountryConfigurationId(Guid Value) : IStronglyTypedId;
