using Hris.SharedKernel;

namespace Hris.Modules.Administration.Domain;

/// <summary>
/// This module's own reusable error catalog, per error-pattern.md's "Error Catalog"
/// section, citing the business-rule IDs from
/// docs/04-modules/administration/domain/business-rules.md in each entry's own
/// remarks where one exists.
///
/// Several rules have no error entry here deliberately, matching every prior
/// module's own documented-gap precedent: AR-024's "scope target must exist and be
/// of the correct kind" requires a live query against the `organization` module,
/// expressed as a caller-supplied boolean the Application layer computes (this
/// platform's own standing "no compile-time cross-module reference" rule);
/// AR-033/AR-034 (bounding a tenant role's composed permissions by the definer's
/// own authority, and disclosing the affected-assignment count before a published
/// role's permissions change) both require live Authorization Framework data this
/// module does not own and this Sprint does not wire, a documented gap tracked in
/// STATUS.md rather than a silently invented check.
/// </summary>
public static class AdministrationErrors
{
    // UserAccount construction.
    public static readonly Error EmployeeIdRequiredForEmployeeLinkedAccount = new(
        "Administration.EmployeeIdRequiredForEmployeeLinkedAccount",
        "An employee-linked account requires an employee identifier.",
        ErrorCategory.Validation);

    public static readonly Error EmployeeIdProhibitedForNonEmployeeLinkedAccount = new(
        "Administration.EmployeeIdProhibitedForNonEmployeeLinkedAccount",
        "Only an employee-linked account may carry an employee identifier. (AR-011)",
        ErrorCategory.Validation);

    public static readonly Error ExpiryDateRequiredForExternalAccount = new(
        "Administration.ExpiryDateRequiredForExternalAccount",
        "An external account requires an expiry date. (AR-017)",
        ErrorCategory.Validation);

    public static readonly Error OwnerRequiredForServiceAccount = new(
        "Administration.OwnerRequiredForServiceAccount",
        "A service account requires a named owner. (AR-018)",
        ErrorCategory.Validation);

    // UserAccount lifecycle.
    public static readonly Error UserAccountNotFound = new(
        "Administration.UserAccountNotFound", "The user account was not found.", ErrorCategory.NotFound);

    public static readonly Error AccountNotPending = new(
        "Administration.AccountNotPending", "This action requires the account to be Pending.", ErrorCategory.Conflict);

    public static readonly Error AccountNotActive = new(
        "Administration.AccountNotActive", "This action requires the account to be Active.", ErrorCategory.Conflict);

    public static readonly Error AccountNotSuspended = new(
        "Administration.AccountNotSuspended", "This action requires the account to be Suspended.", ErrorCategory.Conflict);

    public static readonly Error AccountAlreadyDeprovisioned = new(
        "Administration.AccountAlreadyDeprovisioned", "This account has already been deprovisioned.", ErrorCategory.Conflict);

    public static readonly Error CannotRemoveLastTenantAdministrator = new(
        "Administration.CannotRemoveLastTenantAdministrator",
        "The tenant's last active SystemAdministrator at Tenant scope cannot be removed. (AR-014)",
        ErrorCategory.Conflict);

    // Role assignment / grant.
    public static readonly Error GrantReasonRequired = new(
        "Administration.GrantReasonRequired", "A reason is required for a role grant. (AR-020)", ErrorCategory.Validation);

    public static readonly Error SelfGrantProhibited = new(
        "Administration.SelfGrantProhibited", "A user may not grant a role to their own account. (AR-001)", ErrorCategory.Authorization);

    public static readonly Error InsufficientAuthorityToGrant = new(
        "Administration.InsufficientAuthorityToGrant",
        "The granting user does not hold this role at an equal or wider scope. (AR-002)",
        ErrorCategory.Authorization);

    public static readonly Error ProhibitedRoleCombination = new(
        "Administration.ProhibitedRoleCombination",
        "This grant would produce a role combination prohibited by separation of duties. (AR-003)",
        ErrorCategory.Conflict);

    public static readonly Error DuplicateActiveAssignment = new(
        "Administration.DuplicateActiveAssignment",
        "The same role at the same scope is already actively assigned. (AR-021)",
        ErrorCategory.Conflict);

    public static readonly Error RoleAssignmentNotFound = new(
        "Administration.RoleAssignmentNotFound", "The role assignment was not found.", ErrorCategory.NotFound);

    public static readonly Error ScopeTargetRequired = new(
        "Administration.ScopeTargetRequired", "This scope level requires a target identifier.", ErrorCategory.Validation);

    public static readonly Error ScopeTargetProhibited = new(
        "Administration.ScopeTargetProhibited", "This scope level must not carry a target identifier.", ErrorCategory.Validation);

    // RoleReference.
    public static readonly Error TenantRoleReferenceRequiresPublishedRole = new(
        "Administration.TenantRoleReferenceRequiresPublishedRole",
        "A tenant role reference must resolve to a Published tenant role.",
        ErrorCategory.Validation);

    // TenantRole.
    public static readonly Error TenantRoleNotFound = new(
        "Administration.TenantRoleNotFound", "The tenant role was not found.", ErrorCategory.NotFound);

    public static readonly Error RoleNameRequired = new(
        "Administration.RoleNameRequired", "A tenant role name is required.", ErrorCategory.Validation);

    public static readonly Error RoleNameTooLong = new(
        "Administration.RoleNameTooLong", "The tenant role name exceeds the maximum length.", ErrorCategory.Validation);

    public static readonly Error RoleNameCollidesWithCanonicalRole = new(
        "Administration.RoleNameCollidesWithCanonicalRole",
        "A tenant role name must not match a canonical role name. (AR-030)",
        ErrorCategory.Validation);

    public static readonly Error TenantRoleNotDraft = new(
        "Administration.TenantRoleNotDraft", "This action requires the tenant role to be in Draft status.", ErrorCategory.Conflict);

    public static readonly Error TenantRoleNotPublished = new(
        "Administration.TenantRoleNotPublished",
        "This action requires the tenant role to be Published.",
        ErrorCategory.Conflict);

    public static readonly Error TenantRoleInUseCannotBeDeleted = new(
        "Administration.TenantRoleInUseCannotBeDeleted",
        "A tenant role referenced by an active assignment cannot be deleted. (AR-032)",
        ErrorCategory.Conflict);

    public static readonly Error PermissionReferenceRequired = new(
        "Administration.PermissionReferenceRequired", "A permission reference is required.", ErrorCategory.Validation);

    public static readonly Error DuplicatePermissionGrant = new(
        "Administration.DuplicatePermissionGrant", "This permission is already composed into the tenant role.", ErrorCategory.Conflict);

    public static readonly Error PermissionGrantNotFound = new(
        "Administration.PermissionGrantNotFound", "The permission grant was not found.", ErrorCategory.NotFound);

    // AdministrativeDelegation.
    public static readonly Error DelegationNotFound = new(
        "Administration.DelegationNotFound", "The delegation was not found.", ErrorCategory.NotFound);

    public static readonly Error DelegatorEqualsDelegate = new(
        "Administration.DelegatorEqualsDelegate", "A delegator and delegate must be different accounts.", ErrorCategory.Validation);

    public static readonly Error DelegatedAuthorityEmpty = new(
        "Administration.DelegatedAuthorityEmpty", "A delegation must transfer at least one authority item.", ErrorCategory.Validation);

    public static readonly Error DelegatedAuthorityExceedsDelegatorHoldings = new(
        "Administration.DelegatedAuthorityExceedsDelegatorHoldings",
        "A delegator may only delegate authority they currently hold. (AR-041)",
        ErrorCategory.Authorization);

    public static readonly Error DelegationSeparationOfDutiesViolation = new(
        "Administration.DelegationSeparationOfDutiesViolation",
        "This delegation would give the delegate a role combination prohibited by separation of duties. (AR-045)",
        ErrorCategory.Conflict);

    public static readonly Error DelegationNotScheduled = new(
        "Administration.DelegationNotScheduled", "This action requires the delegation to be Scheduled.", ErrorCategory.Conflict);

    public static readonly Error DelegationNotActive = new(
        "Administration.DelegationNotActive", "This action requires the delegation to be Active.", ErrorCategory.Conflict);

    public static readonly Error DelegationAlreadyTerminal = new(
        "Administration.DelegationAlreadyTerminal", "This delegation has already reached a terminal state.", ErrorCategory.Conflict);
}
