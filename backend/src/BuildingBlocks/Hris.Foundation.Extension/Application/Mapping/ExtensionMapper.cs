using Hris.Foundation.Extension.Application.Dtos;
using Hris.Foundation.Extension.Domain;

namespace Hris.Foundation.Extension.Application.Mapping;

/// <summary>
/// Domain-to-DTO mapping, kept as a plain static class rather than a library such as
/// AutoMapper, per mapping.md's own stated preference for explicit mapping code --
/// the identical choice every other Sprint 3/4 framework's own mapper already
/// establishes.
/// </summary>
internal static class ExtensionMapper
{
    public static ExtensionPointDto ToDto(ExtensionPoint extensionPoint) => new(
        extensionPoint.Id.Value,
        extensionPoint.Key.Value,
        extensionPoint.Name,
        extensionPoint.Description,
        extensionPoint.ExtensionPointType.ToString(),
        extensionPoint.OwningModule,
        extensionPoint.SupportedHookTypes.Select(t => t.ToString()).ToList(),
        extensionPoint.Status.ToString(),
        extensionPoint.Version);

    public static HookDto ToDto(Hook hook) => new(
        hook.Id.Value,
        hook.ExtensionPointId.Value,
        hook.HookType.ToString(),
        hook.HandlerReference,
        hook.OwningModule,
        hook.Status.ToString());
}
