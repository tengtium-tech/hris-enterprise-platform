using Hris.Foundation.Configuration.Application.Dtos;
using Hris.Foundation.Configuration.Domain;

namespace Hris.Foundation.Configuration.Application.Mapping;

/// <summary>
/// Maps <see cref="ConfigurationSetting"/>/<see cref="ConfigurationVersion"/> to their
/// query-side DTOs.
///
/// technology-stack.md's Approved NuGet Packages names Mapster as this platform's
/// object-mapping standard, but this framework deliberately writes these two mappings
/// by hand rather than registering a Mapster <c>IRegister</c> profile for them: every
/// field on both sides requires unwrapping a Value Object (<see cref="ConfigurationKey"/>,
/// <see cref="ConfigurationScope"/>) or converting an enum to its DTO-side string, so a
/// convention-based mapper configuration here would end up as verbose as this file
/// while being less obvious to a reader about which conversions are happening. Stated
/// here as a deviation, per this project's own convention of stating deviations rather
/// than making them silently -- reconsider once a second framework's own query-side
/// DTOs show whether a shared Mapster pattern for Value-Object-heavy aggregates is
/// worth standardizing on instead.
/// </summary>
internal static class ConfigurationMapper
{
    public static ConfigurationSettingDto ToDto(this ConfigurationSetting setting) => new(
        setting.Id.Value,
        setting.Key.Value,
        setting.Scope.Level.ToString(),
        setting.Scope.ScopeId,
        setting.Category.ToString(),
        setting.DataType.ToString(),
        setting.Versions.Select(ToDto).ToList());

    public static ConfigurationVersionDto ToDto(this ConfigurationVersion version) => new(
        version.Id.Value,
        version.VersionNumber,
        version.Value,
        version.EffectiveDate,
        version.ExpirationDate,
        version.ChangeSummary,
        version.State.ToString(),
        version.CreatedByUserId,
        version.ApprovedByUserId,
        version.ApprovedAtUtc);
}
