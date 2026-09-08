using Hris.Modules.Employee.Application.Dtos;
using Hris.Modules.Employee.Domain;

namespace Hris.Modules.Employee.Application.Mapping;

internal static class EmployeeMapper
{
    public static EmployeeDto ToDto(Domain.Employee employee)
    {
        return new EmployeeDto(
            employee.Id.Value,
            employee.TenantId,
            employee.Number.Value,
            employee.Name.FirstName,
            employee.Name.MiddleName,
            employee.Name.LastName,
            employee.Name.Prefix,
            employee.Name.Suffix,
            employee.Name.PreferredName,
            employee.DateOfBirth,
            employee.BirthPlace,
            employee.Gender.ToString(),
            employee.CivilStatus.ToString(),
            employee.Nationality,
            employee.Citizenship,
            employee.PhotographReference,
            employee.SignatureReference,
            employee.PersonalEmail?.Value,
            employee.CompanyEmail?.Value,
            employee.MobileNumber?.Value,
            employee.TelephoneNumber?.Value,
            employee.HomeAddress is null ? null : ToDto(employee.HomeAddress),
            employee.MailingAddress is null ? null : ToDto(employee.MailingAddress),
            employee.Tin?.Value,
            employee.Sss?.Value,
            employee.PhilHealth?.Value,
            employee.PagIbig?.Value,
            employee.Gsis?.Value,
            employee.Banking is null ? null : ToDto(employee.Banking),
            employee.LifecycleStage.ToString(),
            employee.CreatedAtUtc,
            employee.EmergencyContacts.Select(ToDto).ToList(),
            employee.FamilyMembers.Select(ToDto).ToList());
    }

    public static EmployeeSummaryDto ToSummaryDto(Domain.Employee employee)
    {
        return new EmployeeSummaryDto(
            employee.Id.Value, employee.Number.Value, employee.Name.FirstName, employee.Name.LastName, employee.LifecycleStage.ToString());
    }

    private static AddressDto ToDto(Address address) =>
        new(address.AddressLine1, address.AddressLine2, address.City, address.Province, address.PostalCode, address.Country);

    private static BankAccountDto ToDto(BankAccount banking) =>
        new(banking.BankName, banking.Branch, banking.AccountName, banking.AccountNumber, banking.SwiftCode);

    private static EmergencyContactDto ToDto(EmergencyContact contact) =>
        new(contact.Id.Value, contact.Name, contact.Relationship, contact.Phone.Value, contact.Email?.Value, contact.IsPrimary);

    private static FamilyMemberDto ToDto(FamilyMember member) =>
        new(member.Id.Value, member.Name, member.Relationship.ToString(), member.DateOfBirth, member.IsDependent);

    public static EmployeeHistoryDto ToDto(EmployeeHistory history)
    {
        return new EmployeeHistoryDto(
            history.Id.Value, history.EmployeeId, history.Category.ToString(), history.PreviousValue, history.NewValue,
            history.EffectiveDate, history.BusinessReason, history.ChangedBy, history.CreatedAtUtc);
    }
}
