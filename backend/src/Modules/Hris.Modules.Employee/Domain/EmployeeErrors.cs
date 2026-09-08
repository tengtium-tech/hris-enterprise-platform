using Hris.SharedKernel;

namespace Hris.Modules.Employee.Domain;

/// <summary>
/// This module's own reusable error catalog, per error-pattern.md's "Error Catalog"
/// section ("Each bounded context owns its own error catalog"), citing the
/// business-rule IDs from docs/04-modules/employee/domain/business-rules.md in each
/// entry's own remarks where one exists.
///
/// Several rules have no error entry here deliberately, matching Employment's own
/// documented-gap precedent: BR-EMP-028/029's "country-specific validation" and
/// "unique where required by law" for statutory identifiers apply only a generic
/// shape check here (Statutory Reference Data / Rules Engine integration for real
/// per-country format rules is not built this Sprint); BR-EMP-044/045/046's search
/// authorization/visibility rules require the Authorization Framework's own
/// evaluation, expressed at the query layer, not here.
/// </summary>
public static class EmployeeErrors
{
    // Employee Number.
    public static readonly Error EmployeeNumberRequired = new(
        "Employee.NumberRequired", "An employee number is required.", ErrorCategory.Validation);

    public static readonly Error EmployeeNumberTooLong = new(
        "Employee.NumberTooLong", "The employee number exceeds the maximum length.", ErrorCategory.Validation);

    public static readonly Error DuplicateEmployeeNumber = new(
        "Employee.DuplicateNumber", "An employee with this number already exists for the tenant. (BR-EMP-002)", ErrorCategory.Conflict);

    // PersonName.
    public static readonly Error FirstNameRequired = new(
        "Employee.FirstNameRequired", "A first name is required. (BR-EMP-024)", ErrorCategory.Validation);

    public static readonly Error LastNameRequired = new(
        "Employee.LastNameRequired", "A last name is required. (BR-EMP-024)", ErrorCategory.Validation);

    public static readonly Error PersonNameFieldTooLong = new(
        "Employee.PersonNameFieldTooLong", "A person name field exceeds the maximum length.", ErrorCategory.Validation);

    // Personal information.
    public static readonly Error DateOfBirthInFuture = new(
        "Employee.DateOfBirthInFuture", "Date of birth cannot be in the future. (BR-EMP-025)", ErrorCategory.Validation);

    public static readonly Error TextFieldTooLong = new(
        "Employee.TextFieldTooLong", "A text field exceeds the maximum length.", ErrorCategory.Validation);

    // PhoneNumber.
    public static readonly Error PhoneNumberInvalidFormat = new(
        "Employee.PhoneNumberInvalidFormat", "The phone number format is invalid.", ErrorCategory.Validation);

    public static readonly Error PhoneNumberTooLong = new(
        "Employee.PhoneNumberTooLong", "The phone number exceeds the maximum length.", ErrorCategory.Validation);

    // Address.
    public static readonly Error AddressLine1Required = new(
        "Employee.AddressLine1Required", "An address line 1 is required.", ErrorCategory.Validation);

    public static readonly Error AddressCityRequired = new(
        "Employee.AddressCityRequired", "An address city is required.", ErrorCategory.Validation);

    public static readonly Error AddressCountryRequired = new(
        "Employee.AddressCountryRequired", "An address country is required. (value-objects.md: \"Country required\")", ErrorCategory.Validation);

    public static readonly Error AddressFieldTooLong = new(
        "Employee.AddressFieldTooLong", "An address field exceeds the maximum length.", ErrorCategory.Validation);

    // Statutory identifiers (employee-identification.md).
    public static readonly Error TinInvalidFormat = new(
        "Employee.TinInvalidFormat", "The TIN format is invalid.", ErrorCategory.Validation);

    public static readonly Error SssNumberInvalidFormat = new(
        "Employee.SssNumberInvalidFormat", "The SSS number format is invalid.", ErrorCategory.Validation);

    public static readonly Error PhilHealthNumberInvalidFormat = new(
        "Employee.PhilHealthNumberInvalidFormat", "The PhilHealth number format is invalid.", ErrorCategory.Validation);

    public static readonly Error PagIbigNumberInvalidFormat = new(
        "Employee.PagIbigNumberInvalidFormat", "The Pag-IBIG number format is invalid.", ErrorCategory.Validation);

    public static readonly Error GsisNumberInvalidFormat = new(
        "Employee.GsisNumberInvalidFormat", "The GSIS number format is invalid.", ErrorCategory.Validation);

    // BankAccount.
    public static readonly Error BankNameRequired = new(
        "Employee.BankNameRequired", "A bank name is required.", ErrorCategory.Validation);

    public static readonly Error BankAccountNumberRequired = new(
        "Employee.BankAccountNumberRequired", "A bank account number is required.", ErrorCategory.Validation);

    public static readonly Error BankAccountFieldTooLong = new(
        "Employee.BankAccountFieldTooLong", "A banking information field exceeds the maximum length.", ErrorCategory.Validation);

    // Employee lifecycle (BR-EMP-018 through BR-EMP-023).
    public static readonly Error EmployeeNotFound = new(
        "Employee.NotFound", "The employee was not found.", ErrorCategory.NotFound);

    public static readonly Error EmployeeNotHired = new(
        "Employee.NotHired", "This action requires the employee to be in the Hired stage.", ErrorCategory.Conflict);

    public static readonly Error EmployeeNotOnboarding = new(
        "Employee.NotOnboarding", "This action requires the employee to be in the Onboarding stage.", ErrorCategory.Conflict);

    public static readonly Error EmployeeNotActive = new(
        "Employee.NotActive", "This action requires the employee to be Active.", ErrorCategory.Conflict);

    public static readonly Error EmployeeNotOffboarding = new(
        "Employee.NotOffboarding", "This action requires the employee to be in the Offboarding stage.", ErrorCategory.Conflict);

    public static readonly Error EmployeeNotSeparated = new(
        "Employee.NotSeparated", "This action requires the employee to be Separated. (BR-EMP-020)", ErrorCategory.Conflict);

    public static readonly Error EmployeeAlreadyDeceased = new(
        "Employee.AlreadyDeceased", "This employee has already been recorded as deceased. (BR-EMP-020)", ErrorCategory.Conflict);

    public static readonly Error EmployeeRecordIsReadOnly = new(
        "Employee.RecordIsReadOnly",
        "A Separated, Retired, or Deceased employee record is read-only except for lifecycle progression. (employee-lifecycle.md: \"Employee record becomes read-only\")",
        ErrorCategory.Conflict);

    // Emergency contacts (BR-EMP-034 through BR-EMP-036).
    public static readonly Error EmergencyContactNotFound = new(
        "Employee.EmergencyContactNotFound", "The emergency contact was not found.", ErrorCategory.NotFound);

    public static readonly Error EmergencyContactNameRequired = new(
        "Employee.EmergencyContactNameRequired", "An emergency contact name is required.", ErrorCategory.Validation);

    public static readonly Error EmergencyContactRelationshipRequired = new(
        "Employee.EmergencyContactRelationshipRequired", "An emergency contact relationship is required.", ErrorCategory.Validation);

    // Family members.
    public static readonly Error FamilyMemberNotFound = new(
        "Employee.FamilyMemberNotFound", "The family member was not found.", ErrorCategory.NotFound);

    public static readonly Error FamilyMemberNameRequired = new(
        "Employee.FamilyMemberNameRequired", "A family member name is required.", ErrorCategory.Validation);
}
