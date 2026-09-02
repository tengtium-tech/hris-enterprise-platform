namespace Hris.Foundation.Extension.Domain;

/// <summary>
/// A Hook's own subscription lifecycle -- independent of the <see cref="ExtensionPoint"/>
/// it targets, since disabling one handler's subscription must never affect any other
/// handler subscribed to the same point.
/// </summary>
public enum HookStatus
{
    Active = 0,
    Disabled = 1,
    Removed = 2,
}
