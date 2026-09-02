using Hris.SharedKernel;

namespace Hris.Foundation.Extension.Domain;

/// <summary>
/// extension-framework.md's own Domain Events section lists seven events
/// (ExtensionInstalled, ExtensionActivated, ExtensionUpdated, ExtensionDisabled,
/// ExtensionRemoved, PluginValidated, CompatibilityChecked) -- every one of them
/// describes a Plugin/Extension package's own lifecycle (Installed onward, or the
/// Developed/Validated/Packaged stages before it), not the Extension Point contract
/// registry this Sprint actually builds. Installed/Activated/Disabled/Removed map
/// directly to `administration`'s own TenantExtension status values (Phase 3, not yet
/// built); PluginValidated and CompatibilityChecked map to Phase 8's publishing
/// pipeline and TenantExtension's own activation-time compatibility check,
/// respectively. None of the seven belongs here -- these eight events are a new,
/// narrower set describing what <see cref="ExtensionPoint"/> and <see cref="Hook"/>
/// actually do.
/// </summary>
public sealed record ExtensionPointRegistered(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    ExtensionPointId ExtensionPointId,
    ExtensionPointKey Key,
    string Name,
    ExtensionPointType ExtensionPointType,
    string OwningModule) : IDomainEvent;

public sealed record ExtensionPointPublished(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    ExtensionPointId ExtensionPointId,
    int Version) : IDomainEvent;

public sealed record ExtensionPointDeprecated(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    ExtensionPointId ExtensionPointId,
    string Reason) : IDomainEvent;

public sealed record ExtensionPointRetired(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    ExtensionPointId ExtensionPointId) : IDomainEvent;

public sealed record HookRegistered(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    HookId HookId,
    ExtensionPointId ExtensionPointId,
    HookType HookType,
    string HandlerReference,
    string OwningModule) : IDomainEvent;

public sealed record HookDisabled(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    HookId HookId) : IDomainEvent;

public sealed record HookEnabled(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    HookId HookId) : IDomainEvent;

public sealed record HookRemoved(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    HookId HookId) : IDomainEvent;
