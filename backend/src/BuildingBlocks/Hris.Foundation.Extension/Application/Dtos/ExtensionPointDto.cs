namespace Hris.Foundation.Extension.Application.Dtos;

/// <summary>
/// The read-side shape <c>GetExtensionPointQuery</c>/<c>ListExtensionPointsQuery</c>
/// return, per dto-design.md's own convention.
/// </summary>
public sealed record ExtensionPointDto(
    Guid ExtensionPointId,
    string Key,
    string Name,
    string? Description,
    string ExtensionPointType,
    string OwningModule,
    IReadOnlyCollection<string> SupportedHookTypes,
    string Status,
    int Version);
