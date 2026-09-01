namespace Hris.Foundation.Localization.Application.Dtos;

/// <summary>
/// The read-side shape <c>GetCountryConfigurationQuery</c> returns, per
/// dto-design.md's own convention every other framework's own query DTOs already
/// follow: plain primitives crossing the MediatR boundary outward, the mirror image
/// of the raw primitives commands carry inward.
/// </summary>
public sealed record CountryConfigurationDto(
    string Country,
    string DefaultCurrency,
    string DefaultLanguage,
    string DefaultTimeZone,
    IReadOnlyCollection<DayOfWeek> WorkingDays,
    string AddressFormat,
    string PhoneFormat);
