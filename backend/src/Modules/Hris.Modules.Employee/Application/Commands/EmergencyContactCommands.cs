using Hris.Application.Abstractions;
using Hris.Modules.Employee.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Employee.Application.Commands;

public sealed record AddEmergencyContactCommand(
    Guid EmployeeId, Guid TenantId, string? Name, string? Relationship, string? Phone, string? Email, bool IsPrimary)
    : ICommand<Result<Guid>>;

internal sealed class AddEmergencyContactCommandHandler : IRequestHandler<AddEmergencyContactCommand, Result<Guid>>
{
    private readonly IEmployeeRepository _repository;
    private readonly TimeProvider _timeProvider;

    public AddEmergencyContactCommandHandler(IEmployeeRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(AddEmergencyContactCommand request, CancellationToken cancellationToken)
    {
        var employeeResult = await EmployeeLookup.LoadEmployeeForTenantAsync(
            _repository, request.EmployeeId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (employeeResult.IsFailure)
        {
            return Result.Failure<Guid>(employeeResult.Error);
        }

        var addResult = employeeResult.Value.AddEmergencyContact(
            request.Name, request.Relationship, request.Phone, request.Email, request.IsPrimary, _timeProvider.GetUtcNow());
        if (addResult.IsFailure)
        {
            return Result.Failure<Guid>(addResult.Error);
        }

        return Result.Success(employeeResult.Value.EmergencyContacts[^1].Id.Value);
    }
}

public sealed record UpdateEmergencyContactCommand(
    Guid EmployeeId, Guid TenantId, Guid EmergencyContactId, string? Name, string? Relationship, string? Phone, string? Email)
    : ICommand<Result>;

internal sealed class UpdateEmergencyContactCommandHandler : IRequestHandler<UpdateEmergencyContactCommand, Result>
{
    private readonly IEmployeeRepository _repository;
    private readonly TimeProvider _timeProvider;

    public UpdateEmergencyContactCommandHandler(IEmployeeRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(UpdateEmergencyContactCommand request, CancellationToken cancellationToken)
    {
        var employeeResult = await EmployeeLookup.LoadEmployeeForTenantAsync(
            _repository, request.EmployeeId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return employeeResult.IsFailure
            ? Result.Failure(employeeResult.Error)
            : employeeResult.Value.UpdateEmergencyContact(
                request.EmergencyContactId, request.Name, request.Relationship, request.Phone, request.Email,
                _timeProvider.GetUtcNow());
    }
}

public sealed record RemoveEmergencyContactCommand(Guid EmployeeId, Guid TenantId, Guid EmergencyContactId) : ICommand<Result>;

internal sealed class RemoveEmergencyContactCommandHandler : IRequestHandler<RemoveEmergencyContactCommand, Result>
{
    private readonly IEmployeeRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RemoveEmergencyContactCommandHandler(IEmployeeRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RemoveEmergencyContactCommand request, CancellationToken cancellationToken)
    {
        var employeeResult = await EmployeeLookup.LoadEmployeeForTenantAsync(
            _repository, request.EmployeeId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return employeeResult.IsFailure
            ? Result.Failure(employeeResult.Error)
            : employeeResult.Value.RemoveEmergencyContact(request.EmergencyContactId, _timeProvider.GetUtcNow());
    }
}
