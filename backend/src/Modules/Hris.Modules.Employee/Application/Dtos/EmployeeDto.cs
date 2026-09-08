namespace Hris.Modules.Employee.Application.Dtos;

public sealed record EmployeeDto(
    Guid Id,
    Guid TenantId,
    string Number,
    string FirstName,
    string? MiddleName,
    string LastName,
    string? Prefix,
    string? Suffix,
    string? PreferredName,
    DateOnly DateOfBirth,
    string? BirthPlace,
    string Gender,
    string CivilStatus,
    string? Nationality,
    string? Citizenship,
    Guid? PhotographReference,
    Guid? SignatureReference,
    string? PersonalEmail,
    string? CompanyEmail,
    string? MobileNumber,
    string? TelephoneNumber,
    AddressDto? HomeAddress,
    AddressDto? MailingAddress,
    string? Tin,
    string? Sss,
    string? PhilHealth,
    string? PagIbig,
    string? Gsis,
    BankAccountDto? Banking,
    string LifecycleStage,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<EmergencyContactDto> EmergencyContacts,
    IReadOnlyList<FamilyMemberDto> FamilyMembers);

public sealed record EmployeeSummaryDto(Guid Id, string Number, string FirstName, string LastName, string LifecycleStage);

public sealed record AddressDto(
    string AddressLine1, string? AddressLine2, string City, string? Province, string? PostalCode, string Country);

public sealed record BankAccountDto(string BankName, string? Branch, string? AccountName, string AccountNumber, string? SwiftCode);

public sealed record EmergencyContactDto(
    Guid Id, string Name, string Relationship, string Phone, string? Email, bool IsPrimary);

public sealed record FamilyMemberDto(Guid Id, string Name, string Relationship, DateOnly? DateOfBirth, bool IsDependent);
