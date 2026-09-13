using Hris.SharedKernel;

namespace Hris.Modules.Leave.Domain;

/// <summary>
/// Domain error catalog for the Leave module, grouped by aggregate as each is built.
/// Source: docs/04-modules/leave/domain/business-rules.md.
/// </summary>
public static class LeaveErrors
{
    // LeaveType (LV-001 through LV-004).
    public static readonly Error LeaveTypeCodeRequired = new(
        "Leave.LeaveTypeCodeRequired",
        "A leave type requires a code.",
        ErrorCategory.Validation);

    public static readonly Error LeaveTypeNameRequired = new(
        "Leave.LeaveTypeNameRequired",
        "A leave type requires a name.",
        ErrorCategory.Validation);

    public static readonly Error StatutoryBasisRequired = new(
        "Leave.StatutoryBasisRequired",
        "A statutory leave type requires its statutory citation.",
        ErrorCategory.Validation);

    public static readonly Error StatutoryMinimumMustNotBeNegative = new(
        "Leave.StatutoryMinimumMustNotBeNegative",
        "A statutory leave type's minimum entitlement cannot be negative.",
        ErrorCategory.Validation);

    public static readonly Error CannotDeactivateStatutoryLeaveType = new(
        "Leave.CannotDeactivateStatutoryLeaveType",
        "A statutory leave type cannot be deactivated; it remains available platform-wide as long as the underlying law is in force. (LV-004)",
        ErrorCategory.Domain);

    public static readonly Error LeaveTypeNotFound = new(
        "Leave.LeaveTypeNotFound",
        "The requested leave type was not found.",
        ErrorCategory.NotFound);

    // LeavePolicy (LV-010 through LV-015).
    public static readonly Error PolicyVersionNotDraft = new(
        "Leave.PolicyVersionNotDraft",
        "Only a Draft policy version may be published.",
        ErrorCategory.Domain);

    public static readonly Error PolicyVersionNotActive = new(
        "Leave.PolicyVersionNotActive",
        "Only an Active policy version may be revised. (LV-010)",
        ErrorCategory.Domain);

    public static readonly Error PolicyRevisionEffectiveDateMustAdvance = new(
        "Leave.PolicyRevisionEffectiveDateMustAdvance",
        "A policy revision's effective date must be later than the version it supersedes.",
        ErrorCategory.Domain);

    public static readonly Error LeavePolicyNotAssignable = new(
        "Leave.LeavePolicyNotAssignable",
        "Only a Draft or Active policy version may be assigned to a scope.",
        ErrorCategory.Domain);

    public static readonly Error PolicyAssignmentOverlap = new(
        "Leave.PolicyAssignmentOverlap",
        "Two policy assignments to the same scope target may not have overlapping effective periods. (LV-012)",
        ErrorCategory.Domain);

    public static readonly Error PolicyAssignmentNotFound = new(
        "Leave.PolicyAssignmentNotFound",
        "The requested policy assignment was not found.",
        ErrorCategory.NotFound);

    public static readonly Error PolicyBelowStatutoryMinimum = new(
        "Leave.PolicyBelowStatutoryMinimum",
        "A policy against a statutory leave type must configure an entitlement cap at or above that type's statutory minimum. (LV-013)",
        ErrorCategory.Domain);

    public static readonly Error LeavePolicyNotFound = new(
        "Leave.LeavePolicyNotFound",
        "The requested leave policy was not found.",
        ErrorCategory.NotFound);
}
