using Hris.SharedKernel;

namespace Hris.Foundation.Tenant.Domain;

/// <summary>
/// This bounded context's own reusable error catalog, per error-pattern.md's "Error
/// Catalog" section.
/// </summary>
public static class TenantErrors
{
    public static readonly Error TenantCodeRequired = new(
        "Tenant.TenantCodeRequired",
        "A tenant code is required.",
        ErrorCategory.Validation);

    public static readonly Error TenantCodeInvalidFormat = new(
        "Tenant.TenantCodeInvalidFormat",
        "A tenant code must be 3-63 characters, lowercase letters, digits, and internal hyphens only.",
        ErrorCategory.Validation);

    public static readonly Error TenantCodeAlreadyRegistered = new(
        "Tenant.TenantCodeAlreadyRegistered",
        "This tenant code is already registered to another tenant.",
        ErrorCategory.Conflict);

    public static readonly Error OrganizationRequired = new(
        "Tenant.OrganizationRequired",
        "An organization name is required.",
        ErrorCategory.Validation);

    public static readonly Error TenantNotFound = new(
        "Tenant.TenantNotFound",
        "No tenant exists for the given identifier.",
        ErrorCategory.NotFound);

    public static readonly Error InvalidLifecycleTransition = new(
        "Tenant.InvalidLifecycleTransition",
        "This transition is not valid from the tenant's current lifecycle state.",
        ErrorCategory.Domain);

    public static readonly Error DeleteRequiresArchived = new(
        "Tenant.DeleteRequiresArchived",
        "A tenant can only be deleted once it is Archived.",
        ErrorCategory.Domain);

    public static readonly Error SubscriptionPlanChangeRequiresActive = new(
        "Tenant.SubscriptionPlanChangeRequiresActive",
        "A tenant's subscription plan can only be changed while Active.",
        ErrorCategory.Domain);

    public static readonly Error OrganizationNameUpdateRejectedWhenDeleted = new(
        "Tenant.OrganizationNameUpdateRejectedWhenDeleted",
        "A deleted tenant's organization name can no longer be updated.",
        ErrorCategory.Domain);

    public static readonly Error ReasonRequired = new(
        "Tenant.ReasonRequired",
        "A reason is required.",
        ErrorCategory.Validation);

    public static readonly Error ComplianceBasisRequired = new(
        "Tenant.ComplianceBasisRequired",
        "A compliance basis is required to delete a tenant.",
        ErrorCategory.Validation);

    public static readonly Error TenantConfigurationIdRequired = new(
        "Tenant.TenantConfigurationIdRequired",
        "Provisioning cannot complete without a Tenant Configuration already having been created.",
        ErrorCategory.Validation);

    public static readonly Error PlatformOperatorRequired = new(
        "Tenant.PlatformOperatorRequired",
        "This action requires a Platform Operator actor.",
        ErrorCategory.Validation);

    public static readonly Error ActivatedByRequired = new(
        "Tenant.ActivatedByRequired",
        "Activation requires the accepting Tenant Administrator's own account.",
        ErrorCategory.Validation);
}
