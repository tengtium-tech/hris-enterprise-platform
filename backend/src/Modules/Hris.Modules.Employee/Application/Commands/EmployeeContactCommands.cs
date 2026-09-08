using Hris.Application.Abstractions;
using Hris.Modules.Employee.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Employee.Application.Commands;

/// <summary>
/// Updates every contact field (personal/company email, mobile/telephone number,
/// home/mailing address) in one command. commands.md's own "Contact Management"
/// section lists a single <c>UpdateEmployeeContactInformationCommand</c> covering
/// "Address, Email, Phone Number" together, not one command per field.
/// </summary>
public sealed record UpdateEmployeeContactInformationCommand(
    Guid EmployeeId,
    Guid TenantId,
    string? PersonalEmail,
    string? CompanyEmail,
    string? MobileNumber,
    string? TelephoneNumber,
    string? HomeAddressLine1,
    string? HomeAddressLine2,
    string? HomeCity,
    string? HomeProvince,
    string? HomePostalCode,
    string? HomeCountry,
    string? MailingAddressLine1,
    string? MailingAddressLine2,
    string? MailingCity,
    string? MailingProvince,
    string? MailingPostalCode,
    string? MailingCountry) : ICommand<Result>;

internal sealed class UpdateEmployeeContactInformationCommandHandler
    : IRequestHandler<UpdateEmployeeContactInformationCommand, Result>
{
    private readonly IEmployeeRepository _repository;
    private readonly IEmployeeHistoryRepository _historyRepository;
    private readonly TimeProvider _timeProvider;

    public UpdateEmployeeContactInformationCommandHandler(
        IEmployeeRepository repository, IEmployeeHistoryRepository historyRepository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _historyRepository = Guard.AgainstNull(historyRepository, nameof(historyRepository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(UpdateEmployeeContactInformationCommand request, CancellationToken cancellationToken)
    {
        var employeeResult = await EmployeeLookup.LoadEmployeeForTenantAsync(
            _repository, request.EmployeeId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (employeeResult.IsFailure)
        {
            return Result.Failure(employeeResult.Error);
        }

        var previousEmail = employeeResult.Value.PersonalEmail?.Value;
        var nowUtc = _timeProvider.GetUtcNow();

        var updateResult = employeeResult.Value.UpdateContactInformation(
            request.PersonalEmail, request.CompanyEmail, request.MobileNumber, request.TelephoneNumber,
            request.HomeAddressLine1, request.HomeAddressLine2, request.HomeCity, request.HomeProvince,
            request.HomePostalCode, request.HomeCountry, request.MailingAddressLine1, request.MailingAddressLine2,
            request.MailingCity, request.MailingProvince, request.MailingPostalCode, request.MailingCountry, nowUtc);
        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        await EmployeeHistoryRecorder.RecordAsync(
            _historyRepository, request.TenantId, request.EmployeeId, EmployeeHistoryCategory.ContactInformation,
            previousEmail, employeeResult.Value.PersonalEmail?.Value, nowUtc, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
