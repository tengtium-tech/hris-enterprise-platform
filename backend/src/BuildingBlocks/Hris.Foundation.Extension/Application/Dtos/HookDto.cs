namespace Hris.Foundation.Extension.Application.Dtos;

/// <summary>
/// The read-side shape <c>ListHooksForExtensionPointQuery</c> returns, per
/// dto-design.md's own convention.
/// </summary>
public sealed record HookDto(
    Guid HookId,
    Guid ExtensionPointId,
    string HookType,
    string HandlerReference,
    string OwningModule,
    string Status);
