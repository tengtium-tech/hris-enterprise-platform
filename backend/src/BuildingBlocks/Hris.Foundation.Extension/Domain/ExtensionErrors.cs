using Hris.SharedKernel;

namespace Hris.Foundation.Extension.Domain;

/// <summary>
/// This bounded context's own reusable error catalog, per error-pattern.md's "Error
/// Catalog" section.
/// </summary>
public static class ExtensionErrors
{
    public static readonly Error ExtensionPointKeyRequired = new(
        "Extension.ExtensionPointKeyRequired",
        "An extension point key is required.",
        ErrorCategory.Validation);

    public static readonly Error ExtensionPointKeyTooLong = new(
        "Extension.ExtensionPointKeyTooLong",
        "An extension point key cannot exceed 200 characters.",
        ErrorCategory.Validation);

    public static readonly Error ExtensionPointKeyAlreadyRegistered = new(
        "Extension.ExtensionPointKeyAlreadyRegistered",
        "An extension point is already registered under this key.",
        ErrorCategory.Conflict);

    public static readonly Error NameRequired = new(
        "Extension.NameRequired",
        "A name is required.",
        ErrorCategory.Validation);

    public static readonly Error OwningModuleRequired = new(
        "Extension.OwningModuleRequired",
        "An owning module is required.",
        ErrorCategory.Validation);

    public static readonly Error SupportedHookTypesRequired = new(
        "Extension.SupportedHookTypesRequired",
        "At least one supported hook type is required.",
        ErrorCategory.Validation);

    public static readonly Error ExtensionPointNotFound = new(
        "Extension.ExtensionPointNotFound",
        "No extension point exists for the given identifier.",
        ErrorCategory.NotFound);

    public static readonly Error InvalidExtensionPointLifecycleTransition = new(
        "Extension.InvalidExtensionPointLifecycleTransition",
        "This transition is not valid from the extension point's current status.",
        ErrorCategory.Domain);

    public static readonly Error DeprecationReasonRequired = new(
        "Extension.DeprecationReasonRequired",
        "A reason is required to deprecate an extension point.",
        ErrorCategory.Validation);

    public static readonly Error HandlerReferenceRequired = new(
        "Extension.HandlerReferenceRequired",
        "A handler reference is required.",
        ErrorCategory.Validation);

    public static readonly Error HookNotFound = new(
        "Extension.HookNotFound",
        "No hook exists for the given identifier.",
        ErrorCategory.NotFound);

    public static readonly Error InvalidHookLifecycleTransition = new(
        "Extension.InvalidHookLifecycleTransition",
        "This transition is not valid from the hook's current status.",
        ErrorCategory.Domain);

    public static readonly Error ExtensionPointNotPublished = new(
        "Extension.ExtensionPointNotPublished",
        "Only a Published extension point accepts new hook registrations.",
        ErrorCategory.Domain);

    public static readonly Error HookTypeNotSupportedByExtensionPoint = new(
        "Extension.HookTypeNotSupportedByExtensionPoint",
        "This extension point does not support the requested hook type.",
        ErrorCategory.Domain);
}
