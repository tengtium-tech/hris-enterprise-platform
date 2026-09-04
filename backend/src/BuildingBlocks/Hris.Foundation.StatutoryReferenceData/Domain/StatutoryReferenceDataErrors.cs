using Hris.SharedKernel;

namespace Hris.Foundation.StatutoryReferenceData.Domain;

/// <summary>
/// This bounded context's own reusable error catalog, per error-pattern.md's "Error
/// Catalog" section.
/// </summary>
public static class StatutoryReferenceDataErrors
{
    public static readonly Error ProgramCodeRequired = new(
        "StatutoryReferenceData.ProgramCodeRequired",
        "A statutory program code is required.",
        ErrorCategory.Validation);

    public static readonly Error ProgramCodeInvalidFormat = new(
        "StatutoryReferenceData.ProgramCodeInvalidFormat",
        "A statutory program code must be 1-50 uppercase letters, digits, or underscores.",
        ErrorCategory.Validation);

    public static readonly Error CountryCodeRequired = new(
        "StatutoryReferenceData.CountryCodeRequired",
        "A country code is required.",
        ErrorCategory.Validation);

    public static readonly Error CountryCodeInvalidFormat = new(
        "StatutoryReferenceData.CountryCodeInvalidFormat",
        "The country code is not a recognized ISO 3166-1 alpha-2 value.",
        ErrorCategory.Validation);

    public static readonly Error DisplayNameRequired = new(
        "StatutoryReferenceData.DisplayNameRequired",
        "A display name is required.",
        ErrorCategory.Validation);

    public static readonly Error ProgramNotFound = new(
        "StatutoryReferenceData.ProgramNotFound",
        "No statutory program exists for the given identifier.",
        ErrorCategory.NotFound);

    public static readonly Error DuplicateProgramCode = new(
        "StatutoryReferenceData.DuplicateProgramCode",
        "A statutory program with this code already exists for this country.",
        ErrorCategory.Conflict);

    public static readonly Error VersionLabelRequired = new(
        "StatutoryReferenceData.VersionLabelRequired",
        "A table version label is required.",
        ErrorCategory.Validation);

    public static readonly Error VersionLabelInvalidFormat = new(
        "StatutoryReferenceData.VersionLabelInvalidFormat",
        "A table version label must be in YYYY-MM format.",
        ErrorCategory.Validation);

    public static readonly Error DuplicateVersionLabel = new(
        "StatutoryReferenceData.DuplicateVersionLabel",
        "A table version with this label already exists for this program.",
        ErrorCategory.Conflict);

    public static readonly Error EffectiveToBeforeEffectiveFrom = new(
        "StatutoryReferenceData.EffectiveToBeforeEffectiveFrom",
        "The effective-to date cannot precede the effective-from date.",
        ErrorCategory.Validation);

    public static readonly Error ScheduleDataRequired = new(
        "StatutoryReferenceData.ScheduleDataRequired",
        "Table schedule data is required.",
        ErrorCategory.Validation);

    public static readonly Error ScheduleDataMustBeValidJson = new(
        "StatutoryReferenceData.ScheduleDataMustBeValidJson",
        "Table schedule data must be syntactically valid JSON.",
        ErrorCategory.Validation);

    public static readonly Error IssuingAuthorityRequired = new(
        "StatutoryReferenceData.IssuingAuthorityRequired",
        "The issuing authority is required.",
        ErrorCategory.Validation);

    public static readonly Error IssuanceReferenceRequired = new(
        "StatutoryReferenceData.IssuanceReferenceRequired",
        "The issuance reference is required.",
        ErrorCategory.Validation);

    public static readonly Error StatutoryTableVersionNotFound = new(
        "StatutoryReferenceData.StatutoryTableVersionNotFound",
        "No statutory table version exists for the given identifier.",
        ErrorCategory.NotFound);

    public static readonly Error AlreadySignedOff = new(
        "StatutoryReferenceData.AlreadySignedOff",
        "This table version has already received the second-reviewer signoff.",
        ErrorCategory.Domain);

    public static readonly Error SignoffByRequired = new(
        "StatutoryReferenceData.SignoffByRequired",
        "The identity of the second reviewer is required to record a signoff.",
        ErrorCategory.Validation);

    public static readonly Error NoApplicableTableForPeriod = new(
        "StatutoryReferenceData.NoApplicableTableForPeriod",
        "No statutory table version is effective for the given program and period.",
        ErrorCategory.NotFound);

    public static readonly Error NoSignedOffApplicableTableForPeriod = new(
        "StatutoryReferenceData.NoSignedOffApplicableTableForPeriod",
        "A statutory table version is effective for the given program and period, but " +
            "has not yet received the second-reviewer signoff required before it may be " +
            "used for computation.",
        ErrorCategory.Conflict);
}
