namespace Hris.Foundation.Validation.Domain;

/// <summary>
/// The four policies validation-framework.md's Validation Policy section names:
/// "Reject on Error, Continue with Warning, Warning Only, Validation Disabled...
/// should be configurable." Configuration Framework (already built, this Sprint) is
/// where a tenant's chosen policy for a given validation context is actually stored,
/// per that framework's own Security Configuration examples -- this enum is the
/// closed vocabulary that setting's value is drawn from.
/// </summary>
public enum ValidationPolicy
{
    RejectOnError = 0,
    ContinueWithWarning,
    WarningOnly,
    ValidationDisabled,
}
