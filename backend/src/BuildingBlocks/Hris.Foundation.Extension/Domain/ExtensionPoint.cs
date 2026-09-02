using Hris.SharedKernel;

namespace Hris.Foundation.Extension.Domain;

/// <summary>
/// Aggregate Root of the Extension Framework's own published contract registry.
/// Source: docs/03-foundation/extension-framework.md, Core Concepts ("An Extension
/// Point defines where customization is allowed... Only published extension points
/// should be used") and AI Implementation Guidance ("Deliver extension contracts and
/// extension points in Phase 1. They constrain how every module in later phases is
/// written, and cannot be retrofitted without reworking those modules (TC-007)...
/// Expose extension points through stable public contracts.").
///
/// Second framework built in Sprint 4. Deliberately narrower than the document's own
/// full Scope section (Plugin Management, Extension Lifecycle Management, Version
/// Compatibility): this document's own "Plugin" concept -- a package of one or more
/// extensions, independently deployable -- is not this aggregate. Its own lifecycle
/// (Developed -> Validated -> Packaged -> Installed -> Activated -> Updated ->
/// Deprecated -> Removed) is split across two systems that don't exist in code yet:
/// `administration`'s own TenantExtension aggregate (Phase 3) owns Installed onward
/// -- "modeling the lifecycle from Installed onward -- extension-framework.md's own
/// Developed/Validated/Packaged stages belong to Phase 8's publishing pipeline, not to
/// this aggregate" (docs/04-modules/administration/domain/aggregates.md, Tenant
/// Extension Aggregate, Root) -- and Phase 8's Marketplace owns the earlier stages,
/// referenced by TenantExtension only as an opaque "extensionPackageReference." This
/// Sprint's own buildable scope is therefore the one piece with no forward dependency
/// on either: the stable, versioned contract a module registers and a Hook subscribes
/// against.
/// </summary>
public sealed class ExtensionPoint : AggregateRoot<ExtensionPointId>
{
    public ExtensionPointKey Key { get; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public ExtensionPointType ExtensionPointType { get; }

    public string OwningModule { get; }

    public IReadOnlyCollection<HookType> SupportedHookTypes { get; }

    public ExtensionPointStatus Status { get; private set; }

    public int Version { get; }

    private ExtensionPoint(
        ExtensionPointId id,
        ExtensionPointKey key,
        string name,
        string? description,
        ExtensionPointType extensionPointType,
        string owningModule,
        IReadOnlyCollection<HookType> supportedHookTypes)
        : base(id)
    {
        Key = key;
        Name = name;
        Description = description;
        ExtensionPointType = extensionPointType;
        OwningModule = owningModule;
        SupportedHookTypes = supportedHookTypes;
        Status = ExtensionPointStatus.Draft;
        Version = 1;
    }

    /// <summary>
    /// Registers a new extension point in <see cref="ExtensionPointStatus.Draft"/> --
    /// "Only published extension points should be used" means a freshly registered
    /// point is not yet safe for a Hook to target; see <see cref="Publish"/>.
    ///
    /// Global key uniqueness is checked by the caller before this factory runs
    /// (<see cref="IExtensionPointRepository.ExistsByKeyAsync"/>), not here -- the
    /// same split every other framework's own uniqueness-checked factory in this
    /// codebase establishes.
    /// </summary>
    public static Result<ExtensionPoint> Register(
        ExtensionPointKey key,
        string? name,
        string? description,
        ExtensionPointType extensionPointType,
        string? owningModule,
        IReadOnlyCollection<HookType> supportedHookTypes,
        DateTimeOffset nowUtc)
    {
        Guard.AgainstNull(key, nameof(key));
        Guard.AgainstNull(supportedHookTypes, nameof(supportedHookTypes));

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<ExtensionPoint>(ExtensionErrors.NameRequired);
        }

        if (string.IsNullOrWhiteSpace(owningModule))
        {
            return Result.Failure<ExtensionPoint>(ExtensionErrors.OwningModuleRequired);
        }

        if (supportedHookTypes.Count == 0)
        {
            return Result.Failure<ExtensionPoint>(ExtensionErrors.SupportedHookTypesRequired);
        }

        var extensionPoint = new ExtensionPoint(
            new ExtensionPointId(Guid.NewGuid()),
            key,
            name.Trim(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            extensionPointType,
            owningModule.Trim(),
            supportedHookTypes);

        extensionPoint.AddDomainEvent(new ExtensionPointRegistered(
            Guid.NewGuid(), nowUtc, extensionPoint.Id, key, extensionPoint.Name, extensionPointType, extensionPoint.OwningModule));

        return Result.Success(extensionPoint);
    }

    public Result Publish(DateTimeOffset nowUtc)
    {
        if (Status != ExtensionPointStatus.Draft)
        {
            return Result.Failure(ExtensionErrors.InvalidExtensionPointLifecycleTransition);
        }

        Status = ExtensionPointStatus.Published;
        AddDomainEvent(new ExtensionPointPublished(Guid.NewGuid(), nowUtc, Id, Version));
        return Result.Success();
    }

    /// <summary>
    /// A Deprecated point remains usable by any Hook already registered against it --
    /// this method only marks it as scheduled for eventual retirement; no existing
    /// Hook is disabled or removed as a side effect. Retirement (<see cref="Retire"/>)
    /// is the deliberate, later, second step.
    /// </summary>
    public Result Deprecate(string? reason, DateTimeOffset nowUtc)
    {
        if (Status != ExtensionPointStatus.Published)
        {
            return Result.Failure(ExtensionErrors.InvalidExtensionPointLifecycleTransition);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(ExtensionErrors.DeprecationReasonRequired);
        }

        Status = ExtensionPointStatus.Deprecated;
        AddDomainEvent(new ExtensionPointDeprecated(Guid.NewGuid(), nowUtc, Id, reason.Trim()));
        return Result.Success();
    }

    public Result Retire(DateTimeOffset nowUtc)
    {
        if (Status != ExtensionPointStatus.Deprecated)
        {
            return Result.Failure(ExtensionErrors.InvalidExtensionPointLifecycleTransition);
        }

        Status = ExtensionPointStatus.Retired;
        AddDomainEvent(new ExtensionPointRetired(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }
}
