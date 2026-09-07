using Hris.SharedKernel;

namespace Hris.Modules.Organization.Domain;

/// <summary>
/// This module's own reusable error catalog, per error-pattern.md's "Error Catalog"
/// section ("Each bounded context owns its own error catalog"). Names quote
/// docs/04-modules/organization/domain/business-rules.md's own rule identifiers
/// (ORG-xxx, HIER-xxx, BU-xxx, and so on) directly in each entry's remarks so a
/// failing test or a production error code can be traced back to the specific rule
/// it enforces.
///
/// "NotFound" is one error per entity type (matching business-rules.md's own
/// per-type rule numbering), the same granularity IntegrationErrors already
/// establishes for this codebase. "ArchivedCannotBeModified" and "ParentArchived"
/// are shared across every entity type in the Organization Aggregate's own child
/// hierarchy rather than duplicated nine ways: the failure means the identical thing
/// regardless of which level raised it, and the aggregate method that returns it
/// already carries the type-specific context the caller needs.
///
/// DEPT-004 ("A Department with active employees cannot be archived") has no error
/// entry here deliberately: the Employee module does not exist yet
/// (IMPLEMENTATION-PLAN.md, Phase 2 Sprint 4), so this Aggregate has no way to know
/// whether a Department has active employees. Enforcing DEPT-004 is that future
/// Sprint's own Application layer concern, once Employee exists to ask.
/// </summary>
public static class OrganizationErrors
{
    // Value Object validation.
    public static readonly Error OrganizationNameRequired = new(
        "Organization.NameRequired", "An organization name is required.", ErrorCategory.Validation);

    public static readonly Error OrganizationNameTooLong = new(
        "Organization.NameTooLong", "The organization name exceeds the maximum length.", ErrorCategory.Validation);

    public static readonly Error OrganizationCodeRequired = new(
        "Organization.CodeRequired", "An organization code is required.", ErrorCategory.Validation);

    public static readonly Error OrganizationCodeTooLong = new(
        "Organization.CodeTooLong", "The organization code exceeds the maximum length.", ErrorCategory.Validation);

    public static readonly Error BusinessUnitNameRequired = new(
        "Organization.BusinessUnitNameRequired", "A business unit name is required.", ErrorCategory.Validation);

    public static readonly Error DivisionNameRequired = new(
        "Organization.DivisionNameRequired", "A division name is required.", ErrorCategory.Validation);

    public static readonly Error DepartmentNameRequired = new(
        "Organization.DepartmentNameRequired", "A department name is required.", ErrorCategory.Validation);

    public static readonly Error DepartmentCodeRequired = new(
        "Organization.DepartmentCodeRequired", "A department code is required.", ErrorCategory.Validation);

    public static readonly Error SectionNameRequired = new(
        "Organization.SectionNameRequired", "A section name is required.", ErrorCategory.Validation);

    public static readonly Error TeamNameRequired = new(
        "Organization.TeamNameRequired", "A team name is required.", ErrorCategory.Validation);

    public static readonly Error CostCenterCodeRequired = new(
        "Organization.CostCenterCodeRequired", "A cost center code is required.", ErrorCategory.Validation);

    public static readonly Error LocationCodeRequired = new(
        "Organization.LocationCodeRequired", "A work location code is required.", ErrorCategory.Validation);

    public static readonly Error AddressLine1Required = new(
        "Organization.AddressLine1Required", "The first address line is required.", ErrorCategory.Validation);

    public static readonly Error AddressCityRequired = new(
        "Organization.AddressCityRequired", "The address city is required.", ErrorCategory.Validation);

    public static readonly Error AddressCountryRequired = new(
        "Organization.AddressCountryRequired", "The address country is required.", ErrorCategory.Validation);

    public static readonly Error TimeZoneRequired = new(
        "Organization.TimeZoneRequired", "A time zone is required.", ErrorCategory.Validation);

    public static readonly Error TimeZoneInvalid = new(
        "Organization.TimeZoneInvalid",
        "The given value is not a recognized IANA time zone identifier.",
        ErrorCategory.Validation);

    public static readonly Error GeographicCoordinateOutOfRange = new(
        "Organization.GeographicCoordinateOutOfRange",
        "Latitude must be between -90 and 90, and longitude between -180 and 180.",
        ErrorCategory.Validation);

    public static readonly Error LegalEntityCodeRequired = new(
        "Organization.LegalEntityCodeRequired", "A legal entity code is required.", ErrorCategory.Validation);

    public static readonly Error LegalEntityNameRequired = new(
        "Organization.LegalEntityNameRequired", "A legal entity name is required.", ErrorCategory.Validation);

    public static readonly Error BusinessRegistrationNumberRequired = new(
        "Organization.BusinessRegistrationNumberRequired",
        "A business registration number is required.",
        ErrorCategory.Validation);

    public static readonly Error TaxIdentificationNumberRequired = new(
        "Organization.TaxIdentificationNumberRequired",
        "A tax identification number is required.",
        ErrorCategory.Validation);

    // Shared structural errors (Organization Aggregate and every child entity type).
    public static readonly Error ArchivedCannotBeModified = new(
        "Organization.ArchivedCannotBeModified",
        "An archived record cannot be modified. Restore it first.",
        ErrorCategory.Domain);

    public static readonly Error AlreadyArchived = new(
        "Organization.AlreadyArchived", "This record is already archived.", ErrorCategory.Domain);

    public static readonly Error NotArchived = new(
        "Organization.NotArchived", "Only an archived record can be restored.", ErrorCategory.Domain);

    public static readonly Error ParentArchived = new(
        "Organization.ParentArchived",
        "An archived parent cannot accept new child organizational units.",
        ErrorCategory.Domain);

    // Organization (ORG-xxx, HIER-xxx).
    public static readonly Error OrganizationNotFound = new(
        "Organization.OrganizationNotFound", "No organization exists for the given identifier.", ErrorCategory.NotFound);

    public static readonly Error DuplicateOrganizationCode = new(
        "Organization.DuplicateOrganizationCode",
        "ORG-002: another organization in this tenant already uses this code.",
        ErrorCategory.Conflict);

    public static readonly Error DuplicateOrganizationName = new(
        "Organization.DuplicateOrganizationName",
        "ORG-001: another organization in this tenant already uses this name.",
        ErrorCategory.Conflict);

    public static readonly Error CannotArchiveWithActiveChildren = new(
        "Organization.CannotArchiveWithActiveChildren",
        "ORG-003: an organization cannot be archived while active organizational units exist beneath it.",
        ErrorCategory.Domain);

    // Business Unit (BU-xxx).
    public static readonly Error BusinessUnitNotFound = new(
        "Organization.BusinessUnitNotFound",
        "No business unit exists for the given identifier on this organization.",
        ErrorCategory.NotFound);

    public static readonly Error DuplicateBusinessUnitName = new(
        "Organization.DuplicateBusinessUnitName",
        "BU-001: another business unit in this organization already uses this name.",
        ErrorCategory.Conflict);

    // Division (DIV-xxx).
    public static readonly Error DivisionNotFound = new(
        "Organization.DivisionNotFound",
        "No division exists for the given identifier on this organization.",
        ErrorCategory.NotFound);

    public static readonly Error DuplicateDivisionName = new(
        "Organization.DuplicateDivisionName",
        "DIV-001: another division in this business unit already uses this name.",
        ErrorCategory.Conflict);

    public static readonly Error InvalidHierarchyMove = new(
        "Organization.InvalidHierarchyMove",
        "DIV-002/DEPT-003: a unit cannot be moved beneath itself or one of its own descendants.",
        ErrorCategory.Domain);

    // Department (DEPT-xxx).
    public static readonly Error DepartmentNotFound = new(
        "Organization.DepartmentNotFound",
        "No department exists for the given identifier on this organization.",
        ErrorCategory.NotFound);

    public static readonly Error DuplicateDepartmentName = new(
        "Organization.DuplicateDepartmentName",
        "DEPT-001: another department in this division already uses this name.",
        ErrorCategory.Conflict);

    public static readonly Error DuplicateDepartmentCode = new(
        "Organization.DuplicateDepartmentCode",
        "DEPT-002: another department in this tenant already uses this code.",
        ErrorCategory.Conflict);

    public static readonly Error DepartmentMergeRequiresAtLeastTwoSources = new(
        "Organization.DepartmentMergeRequiresAtLeastTwoSources",
        "Merging departments requires at least two source departments.",
        ErrorCategory.Validation);

    public static readonly Error DepartmentSplitRequiresAtLeastTwoTargets = new(
        "Organization.DepartmentSplitRequiresAtLeastTwoTargets",
        "Splitting a department requires at least two new departments.",
        ErrorCategory.Validation);

    // Section (SEC-xxx).
    public static readonly Error SectionNotFound = new(
        "Organization.SectionNotFound",
        "No section exists for the given identifier on this organization.",
        ErrorCategory.NotFound);

    public static readonly Error DuplicateSectionName = new(
        "Organization.DuplicateSectionName",
        "SEC-001: another section in this department already uses this name.",
        ErrorCategory.Conflict);

    // Team (TEAM-xxx).
    public static readonly Error TeamNotFound = new(
        "Organization.TeamNotFound",
        "No team exists for the given identifier on this organization.",
        ErrorCategory.NotFound);

    public static readonly Error DuplicateTeamName = new(
        "Organization.DuplicateTeamName",
        "TEAM-001: another team in this section already uses this name.",
        ErrorCategory.Conflict);

    // Cost Center (CC-xxx).
    public static readonly Error CostCenterNotFound = new(
        "Organization.CostCenterNotFound",
        "No cost center exists for the given identifier on this organization.",
        ErrorCategory.NotFound);

    public static readonly Error DuplicateCostCenterCode = new(
        "Organization.DuplicateCostCenterCode",
        "CC-001: another cost center in this tenant already uses this code.",
        ErrorCategory.Conflict);

    // Work Location (LOC-xxx).
    public static readonly Error WorkLocationNotFound = new(
        "Organization.WorkLocationNotFound", "No work location exists for the given identifier.", ErrorCategory.NotFound);

    public static readonly Error DuplicateLocationCode = new(
        "Organization.DuplicateLocationCode",
        "LOC-001: another work location in this tenant already uses this code.",
        ErrorCategory.Conflict);

    // Legal Entity (LEG-xxx).
    public static readonly Error LegalEntityNotFound = new(
        "Organization.LegalEntityNotFound", "No legal entity exists for the given identifier.", ErrorCategory.NotFound);

    public static readonly Error DuplicateBusinessRegistrationNumber = new(
        "Organization.DuplicateBusinessRegistrationNumber",
        "LEG-001: another legal entity already uses this business registration number.",
        ErrorCategory.Conflict);

    public static readonly Error DuplicateTaxIdentificationNumber = new(
        "Organization.DuplicateTaxIdentificationNumber",
        "LEG-002: another legal entity already uses this tax identification number.",
        ErrorCategory.Conflict);
}
