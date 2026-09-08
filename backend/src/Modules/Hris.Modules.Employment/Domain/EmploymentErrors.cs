using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// This module's own reusable error catalog, per error-pattern.md's "Error Catalog"
/// section ("Each bounded context owns its own error catalog"), citing the
/// business-rule IDs from docs/04-modules/employment/domain/business-rules.md in
/// each entry's own remarks where one exists.
///
/// Several rules have no error entry here deliberately, matching Organization's
/// DEPT-004 and Position's own two-rule precedent of a documented gap rather than an
/// invented check: PROB-001's "Employment Types configured to require probation" has
/// no per-type configuration catalog for this module to check against yet;
/// PROB-003's "at most the number of times permitted by tenant policy" and
/// employment-policies.md's own statutory probation-duration ceiling both require
/// Rules Engine integration this Sprint does not build; EMP-002's "approved Position
/// assignment" and ASG-001's "valid, Active Position" require a live query against
/// the Position module, expressed here as caller-supplied booleans the Application
/// layer computes (matching Position module's own established
/// AssignReportingPosition pattern), not as a compile-time cross-module reference.
/// </summary>
public static class EmploymentErrors
{
    // Value Object validation.
    public static readonly Error EmploymentNumberRequired = new(
        "Employment.NumberRequired", "An employment number is required.", ErrorCategory.Validation);

    public static readonly Error EmploymentNumberTooLong = new(
        "Employment.NumberTooLong", "The employment number exceeds the maximum length.", ErrorCategory.Validation);

    public static readonly Error DuplicateEmploymentNumber = new(
        "Employment.DuplicateNumber", "An employment with this number already exists for the tenant.", ErrorCategory.Conflict);

    public static readonly Error EmploymentTypeRequired = new(
        "Employment.TypeRequired", "An employment type is required.", ErrorCategory.Validation);

    public static readonly Error EmploymentTypeTooLong = new(
        "Employment.TypeTooLong", "The employment type exceeds the maximum length.", ErrorCategory.Validation);

    public static readonly Error EmploymentCategoryRequired = new(
        "Employment.CategoryRequired", "An employment category is required.", ErrorCategory.Validation);

    public static readonly Error EmploymentCategoryTooLong = new(
        "Employment.CategoryTooLong", "The employment category exceeds the maximum length.", ErrorCategory.Validation);

    public static readonly Error ContractTypeRequired = new(
        "Employment.ContractTypeRequired", "A contract type is required.", ErrorCategory.Validation);

    public static readonly Error ContractTypeTooLong = new(
        "Employment.ContractTypeTooLong", "The contract type exceeds the maximum length.", ErrorCategory.Validation);

    public static readonly Error TerminationReasonRequired = new(
        "Employment.TerminationReasonRequired", "A termination reason is required.", ErrorCategory.Validation);

    public static readonly Error TerminationReasonTooLong = new(
        "Employment.TerminationReasonTooLong",
        "The termination reason exceeds the maximum length.",
        ErrorCategory.Validation);

    public static readonly Error ContractPeriodEndBeforeStart = new(
        "Employment.ContractPeriodEndBeforeStart",
        "The contract period end date must not precede the start date.",
        ErrorCategory.Validation);

    public static readonly Error ProbationDurationMustBePositive = new(
        "Employment.ProbationDurationMustBePositive", "Probation duration must be positive.", ErrorCategory.Validation);

    public static readonly Error CompensationAmountNegative = new(
        "Employment.CompensationAmountNegative", "Compensation amount cannot be negative.", ErrorCategory.Validation);

    public static readonly Error CompensationCurrencyCodeInvalid = new(
        "Employment.CompensationCurrencyCodeInvalid",
        "Compensation currency code must be a three-letter ISO 4217 code.",
        ErrorCategory.Validation);

    // Employment lifecycle (EMP-*, STAT-*).
    public static readonly Error EmploymentNotFound = new(
        "Employment.NotFound", "The employment was not found.", ErrorCategory.NotFound);

    public static readonly Error EmploymentSeparatedCannotBeModified = new(
        "Employment.SeparatedCannotBeModified",
        "A separated employment cannot be modified. (EMP-006)",
        ErrorCategory.Conflict);

    public static readonly Error EmploymentNotDraft = new(
        "Employment.NotDraft", "This action requires the employment to be in Draft stage.", ErrorCategory.Conflict);

    public static readonly Error EmploymentActivationRequiresContract = new(
        "Employment.ActivationRequiresContract",
        "Activation requires a valid Employment Contract. (EMP-003)",
        ErrorCategory.Conflict);

    public static readonly Error EmploymentActivationRequiresAssignment = new(
        "Employment.ActivationRequiresAssignment",
        "Activation requires a valid Employment Assignment. (EMP-003)",
        ErrorCategory.Conflict);

    public static readonly Error EmploymentNotActive = new(
        "Employment.NotActive", "This action requires the employment to be Active.", ErrorCategory.Conflict);

    public static readonly Error EmploymentAlreadySeparated = new(
        "Employment.AlreadySeparated", "The employment has already been separated.", ErrorCategory.Conflict);

    public static readonly Error OperationalStatusUnchanged = new(
        "Employment.OperationalStatusUnchanged",
        "The employment already has the requested operational status.",
        ErrorCategory.Conflict);

    public static readonly Error EmploymentNotSuspended = new(
        "Employment.NotSuspended", "This action requires the employment to be currently Suspended.", ErrorCategory.Conflict);

    public static readonly Error SeparationRequiresReason = new(
        "Employment.SeparationRequiresReason", "A separation type is required. (SEP-001)", ErrorCategory.Validation);

    // Probation (PROB-*).
    public static readonly Error ProbationAlreadyStarted = new(
        "Employment.ProbationAlreadyStarted", "Probation has already started for this employment.", ErrorCategory.Conflict);

    public static readonly Error ProbationNotInProgress = new(
        "Employment.ProbationNotInProgress",
        "This action requires an in-progress probation record.",
        ErrorCategory.Conflict);

    public static readonly Error ProbationAlreadyResolved = new(
        "Employment.ProbationAlreadyResolved",
        "This probation record has already been confirmed or failed.",
        ErrorCategory.Conflict);

    // Concurrent employment (CONC-*, EMP-004).
    public static readonly Error ConcurrentEmploymentNotEnabled = new(
        "Employment.ConcurrentEmploymentNotEnabled",
        "Concurrent employment is not enabled for this tenant. (CONC-001)",
        ErrorCategory.Conflict);

    public static readonly Error DuplicatePrimaryEmployment = new(
        "Employment.DuplicatePrimaryEmployment",
        "Only one active primary employment may exist for an employee unless concurrent employment is enabled. (EMP-004)",
        ErrorCategory.Conflict);

    public static readonly Error EmploymentAlreadyPrimary = new(
        "Employment.AlreadyPrimary", "This employment is already the primary employment.", ErrorCategory.Conflict);

    // Employment Contract (CON-*).
    public static readonly Error EmploymentContractNotFound = new(
        "Employment.ContractNotFound", "The employment contract was not found.", ErrorCategory.NotFound);

    public static readonly Error ContractNotDraft = new(
        "Employment.ContractNotDraft", "This action requires the contract to be in Draft stage.", ErrorCategory.Conflict);

    public static readonly Error ContractNotApproved = new(
        "Employment.ContractNotApproved",
        "This action requires the contract to be in Approved stage.",
        ErrorCategory.Conflict);

    public static readonly Error ContractNotEffective = new(
        "Employment.ContractNotEffective",
        "This action requires the contract to be Effective.",
        ErrorCategory.Conflict);

    public static readonly Error ContractNotDraftOrApproved = new(
        "Employment.ContractNotDraftOrApproved",
        "This action requires the contract to be in Draft or Approved stage.",
        ErrorCategory.Conflict);

    public static readonly Error ContractCannotRenewAfterClosed = new(
        "Employment.ContractCannotRenewAfterClosed",
        "A contract cannot be renewed after it has been closed. (CON-004)",
        ErrorCategory.Conflict);

    public static readonly Error FixedTermContractRequiresEndDate = new(
        "Employment.FixedTermContractRequiresEndDate",
        "A fixed-term contract must define an end date. (CON-005)",
        ErrorCategory.Validation);

    public static readonly Error ContractPeriodOverlap = new(
        "Employment.ContractPeriodOverlap",
        "Contract periods for the same employment contract must not overlap. (CON-003)",
        ErrorCategory.Conflict);

    // Employment Assignment (ASG-*).
    public static readonly Error EmploymentAssignmentNotFound = new(
        "Employment.AssignmentNotFound", "The employment assignment was not found.", ErrorCategory.NotFound);

    public static readonly Error AssignmentRequiresActivePosition = new(
        "Employment.AssignmentRequiresActivePosition",
        "An employment assignment must reference a valid, Active position. (ASG-001)",
        ErrorCategory.Conflict);

    public static readonly Error AssignmentAlreadyEnded = new(
        "Employment.AssignmentAlreadyEnded", "This employment assignment has already ended.", ErrorCategory.Conflict);

    public static readonly Error SelfReportingProhibited = new(
        "Employment.SelfReportingProhibited",
        "An employment assignment cannot report to itself.",
        ErrorCategory.Validation);

    public static readonly Error CircularReportingProhibited = new(
        "Employment.CircularReportingProhibited",
        "This reporting assignment would create a circular reporting relationship. (ASG-006)",
        ErrorCategory.Conflict);

    public static readonly Error ReportingManagerEmploymentNotFound = new(
        "Employment.ReportingManagerEmploymentNotFound",
        "The reporting manager's employment was not found.",
        ErrorCategory.NotFound);
}
