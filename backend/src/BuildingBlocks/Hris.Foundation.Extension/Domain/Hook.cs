using Hris.SharedKernel;

namespace Hris.Foundation.Extension.Domain;

/// <summary>
/// Aggregate Root for one handler's own subscription to an <see cref="ExtensionPoint"/>.
/// Source: docs/03-foundation/extension-framework.md, Core Concepts ("Hooks allow
/// execution before or after platform operations... Hooks should not modify unrelated
/// business behavior.").
///
/// A separate Aggregate Root from <see cref="ExtensionPoint"/>, not a child entity of
/// it, for the same population-scale reason <c>PayrollResult</c> is kept independent
/// of <c>PayrollRun</c> elsewhere in this platform: a widely used extension point (for
/// example, a "Before Employee Save" point every tenant's own custom validation
/// extensions might subscribe to) could accumulate many Hooks, and nesting them all
/// inside the point's own aggregate would mean disabling one unrelated handler
/// contends for the same lock as publishing or deprecating the point itself.
///
/// <see cref="HandlerReference"/> is a plain opaque string, not a reference to any
/// real invocable handler type -- no execution engine exists in this Sprint's own
/// build (no business module exists yet to own a handler in the first place), so this
/// aggregate only records the intent to subscribe. Whatever later Sprint or Phase adds
/// real extension execution resolves this reference to something callable; this
/// record is what it resolves against.
/// </summary>
public sealed class Hook : AggregateRoot<HookId>
{
    public ExtensionPointId ExtensionPointId { get; }

    public HookType HookType { get; }

    public string HandlerReference { get; }

    public string OwningModule { get; }

    public HookStatus Status { get; private set; }

    private Hook(
        HookId id,
        ExtensionPointId extensionPointId,
        HookType hookType,
        string handlerReference,
        string owningModule)
        : base(id)
    {
        ExtensionPointId = extensionPointId;
        HookType = hookType;
        HandlerReference = handlerReference;
        OwningModule = owningModule;
        Status = HookStatus.Active;
    }

    /// <summary>
    /// Registers a new, immediately-Active Hook. The caller (the Application-layer
    /// command handler) is responsible for confirming, before calling this factory,
    /// that <paramref name="extensionPointId"/> refers to a real, Published
    /// <see cref="ExtensionPoint"/> whose own <see cref="ExtensionPoint.SupportedHookTypes"/>
    /// includes <paramref name="hookType"/> -- a Value Object/Aggregate factory
    /// validates its own shape, not another aggregate's current state, the same split
    /// established throughout this codebase (see <see cref="ExtensionPoint.Register"/>'s
    /// own remarks for the identical reasoning applied to key uniqueness).
    /// </summary>
    public static Result<Hook> Register(
        ExtensionPointId extensionPointId,
        HookType hookType,
        string? handlerReference,
        string? owningModule,
        DateTimeOffset nowUtc)
    {
        if (extensionPointId.Value == Guid.Empty)
        {
            return Result.Failure<Hook>(ExtensionErrors.ExtensionPointNotFound);
        }

        if (string.IsNullOrWhiteSpace(handlerReference))
        {
            return Result.Failure<Hook>(ExtensionErrors.HandlerReferenceRequired);
        }

        if (string.IsNullOrWhiteSpace(owningModule))
        {
            return Result.Failure<Hook>(ExtensionErrors.OwningModuleRequired);
        }

        var hook = new Hook(
            new HookId(Guid.NewGuid()),
            extensionPointId,
            hookType,
            handlerReference.Trim(),
            owningModule.Trim());

        hook.AddDomainEvent(new HookRegistered(
            Guid.NewGuid(), nowUtc, hook.Id, extensionPointId, hookType, hook.HandlerReference, hook.OwningModule));

        return Result.Success(hook);
    }

    public Result Disable(DateTimeOffset nowUtc)
    {
        if (Status != HookStatus.Active)
        {
            return Result.Failure(ExtensionErrors.InvalidHookLifecycleTransition);
        }

        Status = HookStatus.Disabled;
        AddDomainEvent(new HookDisabled(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result Enable(DateTimeOffset nowUtc)
    {
        if (Status != HookStatus.Disabled)
        {
            return Result.Failure(ExtensionErrors.InvalidHookLifecycleTransition);
        }

        Status = HookStatus.Active;
        AddDomainEvent(new HookEnabled(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    /// <summary>
    /// Terminal, from either <see cref="HookStatus.Active"/> or
    /// <see cref="HookStatus.Disabled"/> -- a removed Hook is never re-enabled; a
    /// handler that wants to subscribe again registers a new Hook.
    /// </summary>
    public Result Remove(DateTimeOffset nowUtc)
    {
        if (Status == HookStatus.Removed)
        {
            return Result.Failure(ExtensionErrors.InvalidHookLifecycleTransition);
        }

        Status = HookStatus.Removed;
        AddDomainEvent(new HookRemoved(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }
}
