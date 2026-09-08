using Hris.SharedKernel;

namespace Hris.Modules.Position.Domain;

/// <summary>
/// This module's own reusable error catalog, per error-pattern.md's "Error Catalog"
/// section ("Each bounded context owns its own error catalog"). Unlike
/// <c>OrganizationErrors</c>, docs/04-modules/position/domain/business-rules.md does
/// not number its own rules (no "POS-001"-style identifiers appear anywhere in this
/// module's own documentation, confirmed by a repository-wide grep before writing
/// this catalog) -- each entry's remarks instead quote the specific rule prose it
/// enforces.
///
/// <see cref="ArchivedCannotBeModified"/> and <see cref="AlreadyArchived"/> are
/// shared across all four of this module's own Aggregate Roots (<see cref="Position"/>,
/// <see cref="JobFamily"/>, <see cref="JobClassification"/>, <see cref="JobGrade"/>),
/// the identical "one error, several entity types" consolidation
/// <c>OrganizationErrors</c> already establishes for its own six-entity hierarchy --
/// the failure means the same thing regardless of which Aggregate raised it.
///
/// Two rules have no error entry here deliberately, matching the Organization
/// module's own DEPT-004 precedent of a documented gap rather than an invented
/// check: "A Draft Position cannot receive employee assignments" and "Only Active
/// Positions may participate in recruitment" (position-lifecycle.md) cannot be
/// enforced by this Aggregate, since neither the Employment nor the Recruitment
/// module exists yet (IMPLEMENTATION-PLAN.md, Phase 2 Sprint 3 onward) -- there is no
/// employee or requisition record for this module to check against.
/// </summary>
public static class PositionErrors
{
    // Value Object validation.
    public static readonly Error PositionNumberRequired = new(
        "Position.NumberRequired", "A position number is required.", ErrorCategory.Validation);

    public static readonly Error PositionNumberTooLong = new(
        "Position.NumberTooLong", "The position number exceeds the maximum length.", ErrorCategory.Validation);

    public static readonly Error PositionTitleRequired = new(
        "Position.TitleRequired", "A position title is required.", ErrorCategory.Validation);

    public static readonly Error PositionTitleTooLong = new(
        "Position.TitleTooLong", "The position title exceeds the maximum length.", ErrorCategory.Validation);

    public static readonly Error PositionTypeRequired = new(
        "Position.TypeRequired", "A position type is required.", ErrorCategory.Validation);

    public static readonly Error PositionTypeTooLong = new(
        "Position.TypeTooLong", "The position type exceeds the maximum length.", ErrorCategory.Validation);

    public static readonly Error AuthorizedHeadcountNegative = new(
        "Position.AuthorizedHeadcountNegative", "Authorized headcount cannot be negative.", ErrorCategory.Validation);

    public static readonly Error JobFamilyCodeRequired = new(
        "Position.JobFamilyCodeRequired", "A job family code is required.", ErrorCategory.Validation);

    public static readonly Error JobFamilyCodeTooLong = new(
        "Position.JobFamilyCodeTooLong", "The job family code exceeds the maximum length.", ErrorCategory.Validation);

    public static readonly Error JobFamilyNameRequired = new(
        "Position.JobFamilyNameRequired", "A job family name is required.", ErrorCategory.Validation);

    public static readonly Error JobFamilyNameTooLong = new(
        "Position.JobFamilyNameTooLong", "The job family name exceeds the maximum length.", ErrorCategory.Validation);

    public static readonly Error JobClassificationCodeRequired = new(
        "Position.JobClassificationCodeRequired", "A job classification code is required.", ErrorCategory.Validation);

    public static readonly Error JobClassificationCodeTooLong = new(
        "Position.JobClassificationCodeTooLong",
        "The job classification code exceeds the maximum length.",
        ErrorCategory.Validation);

    public static readonly Error JobClassificationNameRequired = new(
        "Position.JobClassificationNameRequired", "A job classification name is required.", ErrorCategory.Validation);

    public static readonly Error JobClassificationNameTooLong = new(
        "Position.JobClassificationNameTooLong",
        "The job classification name exceeds the maximum length.",
        ErrorCategory.Validation);

    public static readonly Error JobGradeCodeRequired = new(
        "Position.JobGradeCodeRequired", "A job grade code is required.", ErrorCategory.Validation);

    public static readonly Error JobGradeCodeTooLong = new(
        "Position.JobGradeCodeTooLong", "The job grade code exceeds the maximum length.", ErrorCategory.Validation);

    public static readonly Error JobGradeNameRequired = new(
        "Position.JobGradeNameRequired", "A job grade name is required.", ErrorCategory.Validation);

    public static readonly Error JobGradeNameTooLong = new(
        "Position.JobGradeNameTooLong", "The job grade name exceeds the maximum length.", ErrorCategory.Validation);

    // Not found.
    public static readonly Error PositionNotFound = new(
        "Position.NotFound", "The requested position was not found.", ErrorCategory.NotFound);

    public static readonly Error JobFamilyNotFound = new(
        "Position.JobFamilyNotFound", "The requested job family was not found.", ErrorCategory.NotFound);

    public static readonly Error JobClassificationNotFound = new(
        "Position.JobClassificationNotFound", "The requested job classification was not found.", ErrorCategory.NotFound);

    public static readonly Error JobGradeNotFound = new(
        "Position.JobGradeNotFound", "The requested job grade was not found.", ErrorCategory.NotFound);

    // Uniqueness (Conflict).
    public static readonly Error DuplicatePositionNumber = new(
        "Position.DuplicateNumber",
        "A position with this number already exists. Position numbering.md: \"Duplicate Position Numbers are prohibited.\"",
        ErrorCategory.Conflict);

    public static readonly Error DuplicateJobFamilyCode = new(
        "Position.DuplicateJobFamilyCode",
        "A job family with this code already exists. business-rules.md: \"Job Family codes are unique.\"",
        ErrorCategory.Conflict);

    public static readonly Error DuplicateJobFamilyName = new(
        "Position.DuplicateJobFamilyName",
        "A job family with this name already exists. job-families.md: \"Every Job Family has a unique name.\"",
        ErrorCategory.Conflict);

    public static readonly Error DuplicateJobClassificationCode = new(
        "Position.DuplicateJobClassificationCode",
        "A job classification with this code already exists. business-rules.md: \"Classification codes are unique.\"",
        ErrorCategory.Conflict);

    public static readonly Error DuplicateJobClassificationName = new(
        "Position.DuplicateJobClassificationName",
        "A job classification with this name already exists. "
            + "job-classifications.md: \"Every Job Classification has a unique name.\"",
        ErrorCategory.Conflict);

    public static readonly Error DuplicateJobGradeCode = new(
        "Position.DuplicateJobGradeCode",
        "A job grade with this code already exists. business-rules.md: \"Grade Codes are unique.\"",
        ErrorCategory.Conflict);

    public static readonly Error DuplicateJobGradeName = new(
        "Position.DuplicateJobGradeName",
        "A job grade with this name already exists. job-grades.md: \"Every Job Grade has a unique name.\"",
        ErrorCategory.Conflict);

    // Lifecycle, shared across all four Aggregate Roots.
    public static readonly Error ArchivedCannotBeModified = new(
        "Position.ArchivedCannotBeModified",
        "An archived record cannot be modified. position-policies.md: \"Archived Positions... "
            + "cannot receive new employee assignments... cannot participate in recruitment.\"",
        ErrorCategory.Domain);

    public static readonly Error AlreadyArchived = new(
        "Position.AlreadyArchived", "This record is already archived.", ErrorCategory.Domain);

    // Position lifecycle.
    public static readonly Error PositionAlreadyActive = new(
        "Position.AlreadyActive", "This position is already active.", ErrorCategory.Domain);

    public static readonly Error PositionNotActive = new(
        "Position.NotActive",
        "This position is not active. position-lifecycle.md: supported transitions require "
            + "the position to be Active before it can be deactivated.",
        ErrorCategory.Domain);

    public static readonly Error PositionCannotArchiveDraft = new(
        "Position.CannotArchiveDraft",
        "A draft position must be activated or deactivated before it can be archived. "
            + "position-lifecycle.md's own State Transitions section lists no Draft-to-Archived path.",
        ErrorCategory.Domain);

    // Position hierarchy.
    public static readonly Error SelfReportingProhibited = new(
        "Position.SelfReportingProhibited",
        "A position cannot report to itself. position-hierarchy.md: \"Self-reporting is prohibited.\"",
        ErrorCategory.Domain);

    public static readonly Error ReportingPositionNotFound = new(
        "Position.ReportingPositionNotFound",
        "The reporting position was not found. position-hierarchy.md: \"Parent Positions must exist.\"",
        ErrorCategory.Domain);

    public static readonly Error ReportingPositionMustBeActive = new(
        "Position.ReportingPositionMustBeActive",
        "The reporting position must be active. position-hierarchy.md: \"Archived Positions cannot "
            + "become Parent Positions.\"",
        ErrorCategory.Domain);

    public static readonly Error CircularReportingProhibited = new(
        "Position.CircularReportingProhibited",
        "Assigning this reporting position would create a circular reporting structure. "
            + "position-hierarchy.md: \"Circular references are prohibited.\"",
        ErrorCategory.Domain);

    public static readonly Error NoReportingPositionToRemove = new(
        "Position.NoReportingPositionToRemove", "This position has no reporting position to remove.", ErrorCategory.Domain);

    // Vacancy.
    public static readonly Error PositionAlreadyVacant = new(
        "Position.AlreadyVacant", "This position is already marked vacant.", ErrorCategory.Domain);

    public static readonly Error PositionAlreadyFilled = new(
        "Position.AlreadyFilled", "This position is already marked filled.", ErrorCategory.Domain);

    // Workforce classification lifecycle (JobFamily, JobClassification, JobGrade).
    public static readonly Error WorkforceClassificationAlreadyActive = new(
        "Position.WorkforceClassificationAlreadyActive", "This record is already active.", ErrorCategory.Domain);

    public static readonly Error WorkforceClassificationAlreadyInactive = new(
        "Position.WorkforceClassificationAlreadyInactive", "This record is already inactive.", ErrorCategory.Domain);
}
