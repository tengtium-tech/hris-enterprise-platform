using Hris.Foundation.Localization.Application.Dtos;
using Hris.Foundation.Localization.Domain;

namespace Hris.Foundation.Localization.Application.Mapping;

/// <summary>
/// Domain-to-DTO mapping, kept as a plain static class rather than a library such as
/// AutoMapper, per mapping.md's own stated preference for explicit mapping code in a
/// codebase this size -- the identical choice <c>RuleMapper</c> already establishes
/// for a sibling framework.
/// </summary>
internal static class LocalizationMapper
{
    public static CountryConfigurationDto ToDto(CountryConfiguration configuration) => new(
        configuration.Country.Value,
        configuration.DefaultCurrency.Value,
        configuration.DefaultLanguage.Value,
        configuration.DefaultTimeZone.Value,
        configuration.WorkingDays.ToList(),
        configuration.AddressFormat,
        configuration.PhoneFormat);
}
