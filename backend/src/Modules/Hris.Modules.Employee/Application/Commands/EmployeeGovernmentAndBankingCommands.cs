using Hris.Application.Abstractions;
using Hris.Modules.Employee.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Employee.Application.Commands;

public sealed record UpdateEmployeeGovernmentInformationCommand(
    Guid EmployeeId, Guid TenantId, string? Tin, string? Sss, string? PhilHealth, string? PagIbig, string? Gsis) : ICommand<Result>;

internal sealed class UpdateEmployeeGovernmentInformationCommandHandler
    : IRequestHandler<UpdateEmployeeGovernmentInformationCommand, Result>
{
    private readonly IEmployeeRepository _repository;
    private readonly IEmployeeHistoryRepository _historyRepository;
    private readonly TimeProvider _timeProvider;

    public UpdateEmployeeGovernmentInformationCommandHandler(
        IEmployeeRepository repository, IEmployeeHistoryRepository historyRepository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _historyRepository = Guard.AgainstNull(historyRepository, nameof(historyRepository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(UpdateEmployeeGovernmentInformationCommand request, CancellationToken cancellationToken)
    {
        var employeeResult = await EmployeeLookup.LoadEmployeeForTenantAsync(
            _repository, request.EmployeeId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (employeeResult.IsFailure)
        {
            return Result.Failure(employeeResult.Error);
        }

        var previousTin = employeeResult.Value.Tin?.Value;
        var nowUtc = _timeProvider.GetUtcNow();

        var updateResult = employeeResult.Value.UpdateGovernmentInformation(
            request.Tin, request.Sss, request.PhilHealth, request.PagIbig, request.Gsis, nowUtc);
        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        await EmployeeHistoryRecorder.RecordAsync(
            _historyRepository, request.TenantId, request.EmployeeId, EmployeeHistoryCategory.GovernmentInformation,
            previousTin, employeeResult.Value.Tin?.Value, nowUtc, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}

public sealed record UpdateEmployeeBankInformationCommand(
    Guid EmployeeId, Guid TenantId, string? BankName, string? Branch, string? AccountName, string? AccountNumber,
    string? SwiftCode) : ICommand<Result>;

internal sealed class UpdateEmployeeBankInformationCommandHandler : IRequestHandler<UpdateEmployeeBankInformationCommand, Result>
{
    private readonly IEmployeeRepository _repository;
    private readonly IEmployeeHistoryRepository _historyRepository;
    private readonly TimeProvider _timeProvider;

    public UpdateEmployeeBankInformationCommandHandler(
        IEmployeeRepository repository, IEmployeeHistoryRepository historyRepository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _historyRepository = Guard.AgainstNull(historyRepository, nameof(historyRepository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(UpdateEmployeeBankInformationCommand request, CancellationToken cancellationToken)
    {
        var employeeResult = await EmployeeLookup.LoadEmployeeForTenantAsync(
            _repository, request.EmployeeId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (employeeResult.IsFailure)
        {
            return Result.Failure(employeeResult.Error);
        }

        var previousAccountNumber = employeeResult.Value.Banking?.AccountNumber;
        var nowUtc = _timeProvider.GetUtcNow();

        var updateResult = employeeResult.Value.UpdateBankingInformation(
            request.BankName, request.Branch, request.AccountName, request.AccountNumber, request.SwiftCode, nowUtc);
        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        await EmployeeHistoryRecorder.RecordAsync(
            _historyRepository, request.TenantId, request.EmployeeId, EmployeeHistoryCategory.BankingInformation,
            previousAccountNumber, employeeResult.Value.Banking?.AccountNumber, nowUtc, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
