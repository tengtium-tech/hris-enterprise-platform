using Hris.Application.Abstractions;
using Hris.Modules.Employee.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Employee.Application.Commands;

public sealed record AddFamilyMemberCommand(
    Guid EmployeeId, Guid TenantId, string? Name, FamilyRelationship Relationship, DateOnly? DateOfBirth, bool IsDependent)
    : ICommand<Result<Guid>>;

internal sealed class AddFamilyMemberCommandHandler : IRequestHandler<AddFamilyMemberCommand, Result<Guid>>
{
    private readonly IEmployeeRepository _repository;
    private readonly TimeProvider _timeProvider;

    public AddFamilyMemberCommandHandler(IEmployeeRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(AddFamilyMemberCommand request, CancellationToken cancellationToken)
    {
        var employeeResult = await EmployeeLookup.LoadEmployeeForTenantAsync(
            _repository, request.EmployeeId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (employeeResult.IsFailure)
        {
            return Result.Failure<Guid>(employeeResult.Error);
        }

        var addResult = employeeResult.Value.AddFamilyMember(
            request.Name, request.Relationship, request.DateOfBirth, request.IsDependent, _timeProvider.GetUtcNow());
        if (addResult.IsFailure)
        {
            return Result.Failure<Guid>(addResult.Error);
        }

        return Result.Success(employeeResult.Value.FamilyMembers[^1].Id.Value);
    }
}

public sealed record UpdateFamilyMemberCommand(
    Guid EmployeeId, Guid TenantId, Guid FamilyMemberId, string? Name, FamilyRelationship Relationship,
    DateOnly? DateOfBirth, bool IsDependent) : ICommand<Result>;

internal sealed class UpdateFamilyMemberCommandHandler : IRequestHandler<UpdateFamilyMemberCommand, Result>
{
    private readonly IEmployeeRepository _repository;
    private readonly TimeProvider _timeProvider;

    public UpdateFamilyMemberCommandHandler(IEmployeeRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(UpdateFamilyMemberCommand request, CancellationToken cancellationToken)
    {
        var employeeResult = await EmployeeLookup.LoadEmployeeForTenantAsync(
            _repository, request.EmployeeId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return employeeResult.IsFailure
            ? Result.Failure(employeeResult.Error)
            : employeeResult.Value.UpdateFamilyMember(
                request.FamilyMemberId, request.Name, request.Relationship, request.DateOfBirth, request.IsDependent,
                _timeProvider.GetUtcNow());
    }
}

public sealed record RemoveFamilyMemberCommand(Guid EmployeeId, Guid TenantId, Guid FamilyMemberId) : ICommand<Result>;

internal sealed class RemoveFamilyMemberCommandHandler : IRequestHandler<RemoveFamilyMemberCommand, Result>
{
    private readonly IEmployeeRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RemoveFamilyMemberCommandHandler(IEmployeeRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RemoveFamilyMemberCommand request, CancellationToken cancellationToken)
    {
        var employeeResult = await EmployeeLookup.LoadEmployeeForTenantAsync(
            _repository, request.EmployeeId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return employeeResult.IsFailure
            ? Result.Failure(employeeResult.Error)
            : employeeResult.Value.RemoveFamilyMember(request.FamilyMemberId, _timeProvider.GetUtcNow());
    }
}
