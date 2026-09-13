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
}
