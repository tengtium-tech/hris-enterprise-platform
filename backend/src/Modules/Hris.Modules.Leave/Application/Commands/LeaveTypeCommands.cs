using Hris.Application.Abstractions;
using Hris.Modules.Leave.Application;
using Hris.Modules.Leave.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Leave.Application.Commands;

/// <summary>
/// Defines a tenant-owned company leave type (LV-002). There is deliberately no
/// tenant-facing command that creates a statutory type — those are platform-seeded.
/// Source: application/commands.md (LeaveType Commands).
/// </summary>
public sealed record DefineLeaveTypeCommand(
    Guid TenantId, string Code, string Name, Guid ActorId) : ICommand<Result<Guid>>;

internal sealed class DefineLeaveTypeCommandHandler : IRequestHandler<DefineLeaveTypeCommand, Result<Guid>>
{
    private readonly ILeaveTypeRepository _repository;
    private readonly TimeProvider _timeProvider;

    public DefineLeaveTypeCommandHandler(ILeaveTypeRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(DefineLeaveTypeCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var createResult = LeaveType.DefineTenantType(
            new LeaveTypeId(Guid.NewGuid()), request.TenantId, request.Code, request.Name, request.ActorId,
            _timeProvider.GetUtcNow());

        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Error);
        }

        await _repository.AddAsync(createResult.Value, cancellationToken).ConfigureAwait(false);
        return Result.Success(createResult.Value.Id.Value);
    }
}

/// <summary>Deactivates a tenant-defined leave type; rejected for a statutory type (LV-004).</summary>
public sealed record DeactivateLeaveTypeCommand(
    Guid TenantId, Guid LeaveTypeId, Guid ActorId, string Reason) : ICommand<Result>;

internal sealed class DeactivateLeaveTypeCommandHandler : IRequestHandler<DeactivateLeaveTypeCommand, Result>
{
    private readonly ILeaveTypeRepository _repository;
    private readonly TimeProvider _timeProvider;

    public DeactivateLeaveTypeCommandHandler(ILeaveTypeRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(DeactivateLeaveTypeCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var typeResult = await LeaveLookup
            .LoadLeaveTypeForTenantAsync(_repository, request.LeaveTypeId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (typeResult.IsFailure)
        {
            return Result.Failure(typeResult.Error);
        }

        return typeResult.Value.Deactivate(request.ActorId, request.Reason, _timeProvider.GetUtcNow());
    }
}
