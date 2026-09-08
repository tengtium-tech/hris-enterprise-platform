using Hris.Application.Abstractions;
using Hris.Modules.Employee.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Employee.Application.Commands;

/// <summary>
/// Creates a new Employee starting at <see cref="EmployeeLifecycleStage.Hired"/>.
/// Hire Date, Employment Type, Organization, and Position are deliberately absent
/// -- see <c>Domain.Employee.Create</c>'s own remarks and commands.md's Employee
/// Registration section.
/// </summary>
public sealed record RegisterEmployeeCommand(
    Guid TenantId,
    string? Number,
    string? FirstName,
    string? MiddleName,
    string? LastName,
    string? Prefix,
    string? Suffix,
    string? PreferredName,
    DateOnly DateOfBirth,
    string? BirthPlace,
    Gender Gender,
    CivilStatus CivilStatus,
    string? Nationality,
    string? Citizenship) : ICommand<Result<Guid>>;

internal sealed class RegisterEmployeeCommandHandler : IRequestHandler<RegisterEmployeeCommand, Result<Guid>>
{
    private readonly IEmployeeRepository _repository;
    private readonly IEmployeeHistoryRepository _historyRepository;
    private readonly TimeProvider _timeProvider;

    public RegisterEmployeeCommandHandler(
        IEmployeeRepository repository, IEmployeeHistoryRepository historyRepository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _historyRepository = Guard.AgainstNull(historyRepository, nameof(historyRepository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(RegisterEmployeeCommand request, CancellationToken cancellationToken)
    {
        if (request.Number is not null
            && await _repository.ExistsWithNumberAsync(request.TenantId, request.Number, null, cancellationToken)
                .ConfigureAwait(false))
        {
            return Result.Failure<Guid>(EmployeeErrors.DuplicateEmployeeNumber);
        }

        var id = new EmployeeId(Guid.NewGuid());
        var nowUtc = _timeProvider.GetUtcNow();

        var employeeResult = Domain.Employee.Create(
            id, request.TenantId, request.Number, request.FirstName, request.MiddleName, request.LastName, request.Prefix,
            request.Suffix, request.PreferredName, request.DateOfBirth, request.BirthPlace, request.Gender,
            request.CivilStatus, request.Nationality, request.Citizenship, nowUtc);
        if (employeeResult.IsFailure)
        {
            return Result.Failure<Guid>(employeeResult.Error);
        }

        await _repository.AddAsync(employeeResult.Value, cancellationToken).ConfigureAwait(false);
        await EmployeeHistoryRecorder.RecordAsync(
            _historyRepository, request.TenantId, id.Value, EmployeeHistoryCategory.LifecycleStage, null,
            EmployeeLifecycleStage.Hired.ToString(), nowUtc, cancellationToken).ConfigureAwait(false);

        return Result.Success(employeeResult.Value.Id.Value);
    }
}

public sealed record UpdateEmployeePersonalInformationCommand(
    Guid EmployeeId,
    Guid TenantId,
    string? FirstName,
    string? MiddleName,
    string? LastName,
    string? Prefix,
    string? Suffix,
    string? PreferredName,
    DateOnly DateOfBirth,
    string? BirthPlace,
    Gender Gender,
    CivilStatus CivilStatus,
    string? Nationality,
    string? Citizenship) : ICommand<Result>;

internal sealed class UpdateEmployeePersonalInformationCommandHandler
    : IRequestHandler<UpdateEmployeePersonalInformationCommand, Result>
{
    private readonly IEmployeeRepository _repository;
    private readonly IEmployeeHistoryRepository _historyRepository;
    private readonly TimeProvider _timeProvider;

    public UpdateEmployeePersonalInformationCommandHandler(
        IEmployeeRepository repository, IEmployeeHistoryRepository historyRepository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _historyRepository = Guard.AgainstNull(historyRepository, nameof(historyRepository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(UpdateEmployeePersonalInformationCommand request, CancellationToken cancellationToken)
    {
        var employeeResult = await EmployeeLookup.LoadEmployeeForTenantAsync(
            _repository, request.EmployeeId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (employeeResult.IsFailure)
        {
            return Result.Failure(employeeResult.Error);
        }

        var previousName = employeeResult.Value.Name.ToString();
        var nowUtc = _timeProvider.GetUtcNow();

        var updateResult = employeeResult.Value.UpdatePersonalInformation(
            request.FirstName, request.MiddleName, request.LastName, request.Prefix, request.Suffix, request.PreferredName,
            request.DateOfBirth, request.BirthPlace, request.Gender, request.CivilStatus, request.Nationality,
            request.Citizenship, nowUtc);
        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        await EmployeeHistoryRecorder.RecordAsync(
            _historyRepository, request.TenantId, request.EmployeeId, EmployeeHistoryCategory.PersonalInformation,
            previousName, employeeResult.Value.Name.ToString(), nowUtc, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}

public sealed record UpdateEmployeePhotoCommand(Guid EmployeeId, Guid TenantId, Guid? PhotographReference) : ICommand<Result>;

internal sealed class UpdateEmployeePhotoCommandHandler : IRequestHandler<UpdateEmployeePhotoCommand, Result>
{
    private readonly IEmployeeRepository _repository;
    private readonly TimeProvider _timeProvider;

    public UpdateEmployeePhotoCommandHandler(IEmployeeRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(UpdateEmployeePhotoCommand request, CancellationToken cancellationToken)
    {
        var employeeResult = await EmployeeLookup.LoadEmployeeForTenantAsync(
            _repository, request.EmployeeId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return employeeResult.IsFailure
            ? Result.Failure(employeeResult.Error)
            : employeeResult.Value.UpdatePhoto(request.PhotographReference, _timeProvider.GetUtcNow());
    }
}
