using Hris.Foundation.Extension.Domain;

namespace Hris.Foundation.Extension.Tests;

/// <summary>
/// Valid-default builders per docs/09-testing/unit-and-integration-testing.md 2.4:
/// "Construct aggregates through builders that supply valid defaults, so each test
/// specifies only the values relevant to what it verifies." A fixed clock
/// (<see cref="NowUtc"/>), never <c>DateTimeOffset.UtcNow</c>, per that same
/// document's own 2.1 ("must not touch... a clock").
/// </summary>
internal static class TestData
{
    public static readonly DateTimeOffset NowUtc = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    public static ExtensionPointKey NewKey(string? value = null) =>
        ExtensionPointKey.Create(value ?? $"employee.before-save.{Guid.NewGuid():N}").Value;

    public static ExtensionPoint RegisteredExtensionPoint(
        ExtensionPointKey? key = null,
        string name = "Before Employee Save",
        string? description = "Runs immediately before an Employee record is persisted.",
        ExtensionPointType extensionPointType = ExtensionPointType.BusinessLogic,
        string owningModule = "Employee",
        IReadOnlyCollection<HookType>? supportedHookTypes = null,
        DateTimeOffset? nowUtc = null) =>
        ExtensionPoint.Register(
            key ?? NewKey(),
            name,
            description,
            extensionPointType,
            owningModule,
            supportedHookTypes ?? [HookType.Before, HookType.After],
            nowUtc ?? NowUtc).Value;

    /// <summary>An extension point already <see cref="ExtensionPointStatus.Published"/>.</summary>
    public static ExtensionPoint PublishedExtensionPoint(DateTimeOffset? nowUtc = null)
    {
        var extensionPoint = RegisteredExtensionPoint(nowUtc: nowUtc);
        extensionPoint.Publish(nowUtc ?? NowUtc);
        return extensionPoint;
    }

    /// <summary>An extension point already <see cref="ExtensionPointStatus.Deprecated"/>.</summary>
    public static ExtensionPoint DeprecatedExtensionPoint(DateTimeOffset? nowUtc = null)
    {
        var extensionPoint = PublishedExtensionPoint(nowUtc);
        extensionPoint.Deprecate("Superseded by a newer point.", nowUtc ?? NowUtc);
        return extensionPoint;
    }

    /// <summary>An extension point already <see cref="ExtensionPointStatus.Retired"/>.</summary>
    public static ExtensionPoint RetiredExtensionPoint(DateTimeOffset? nowUtc = null)
    {
        var extensionPoint = DeprecatedExtensionPoint(nowUtc);
        extensionPoint.Retire(nowUtc ?? NowUtc);
        return extensionPoint;
    }

    public static Hook RegisteredHook(
        ExtensionPointId? extensionPointId = null,
        HookType hookType = HookType.Before,
        string handlerReference = "Modules.Employee.Handlers.ValidateEmployeeNumberFormat",
        string owningModule = "Employee",
        DateTimeOffset? nowUtc = null) =>
        Hook.Register(
            extensionPointId ?? new ExtensionPointId(Guid.NewGuid()),
            hookType,
            handlerReference,
            owningModule,
            nowUtc ?? NowUtc).Value;

    /// <summary>A hook already <see cref="HookStatus.Disabled"/>.</summary>
    public static Hook DisabledHook(DateTimeOffset? nowUtc = null)
    {
        var hook = RegisteredHook(nowUtc: nowUtc);
        hook.Disable(nowUtc ?? NowUtc);
        return hook;
    }

    /// <summary>A hook already <see cref="HookStatus.Removed"/>.</summary>
    public static Hook RemovedHook(DateTimeOffset? nowUtc = null)
    {
        var hook = RegisteredHook(nowUtc: nowUtc);
        hook.Remove(nowUtc ?? NowUtc);
        return hook;
    }
}
