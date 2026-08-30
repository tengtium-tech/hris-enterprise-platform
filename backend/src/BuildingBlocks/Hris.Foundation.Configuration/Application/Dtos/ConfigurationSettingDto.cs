namespace Hris.Foundation.Configuration.Application.Dtos;

/// <summary>
/// The read-side shape of a <see cref="Domain.ConfigurationSetting"/>, per
/// application-pipeline.md's Query Handlers section: "Execute read operations. Return
/// DTOs." Primitive-only (no Domain Value Objects, no strongly typed ids) since this is
/// the shape a future API Platform (Sprint 7) endpoint would serialize directly --
/// coding-standards.md's Application Layer convention keeps Commands able to reference
/// Domain types freely, but a query's own output should not force a Presentation-layer
/// caller to understand this framework's internal Value Object shapes.
/// </summary>
public sealed record ConfigurationSettingDto(
    Guid Id,
    string Key,
    string ScopeLevel,
    Guid? ScopeId,
    string Category,
    string DataType,
    IReadOnlyList<ConfigurationVersionDto> Versions);

/// <summary>
/// The read-side shape of a <see cref="Domain.ConfigurationVersion"/>.
/// </summary>
public sealed record ConfigurationVersionDto(
    Guid Id,
    int VersionNumber,
    string Value,
    DateOnly EffectiveDate,
    DateOnly? ExpirationDate,
    string ChangeSummary,
    string State,
    Guid CreatedByUserId,
    Guid? ApprovedByUserId,
    DateTimeOffset? ApprovedAtUtc);
