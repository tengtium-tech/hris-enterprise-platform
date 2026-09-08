using Hris.Application.Abstractions;
using Hris.Modules.Employment.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Employment.Application.Commands;

/// <summary>
/// Creates the Employment Assignment (AssignPositionCommand in commands.md).
/// <see cref="PositionIsActive"/> is caller-supplied rather than resolved via a
/// cross-module Position lookup this Sprint does not build -- see
/// <see cref="MovementType"/>'s own remarks for why, applied here to ASG-001 the
/// same way it applies to promotion/demotion classification.
/// </summary>
public sealed record AssignPositionCommand(
    Guid EmploymentId,
    Guid TenantId,
    Guid PositionId,
    Guid? DepartmentId,
    Guid? BusinessUnitId,
    Guid? CostCenterId,
    Guid? WorkLocationId,
    Guid? LegalEntityId,
    WorkArrangement WorkArrangement,
    Guid? ReportingManagerEmploymentId,
    DateOnly EffectiveStartDate,
    bool PositionIsActive) : ICommand<Result<Guid>>;

internal sealed class AssignPositionCommandHandler : IRequestHandler<AssignPositionCommand, Result<Guid>>
{
    private readonly IEmploymentAssignmentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public AssignPositionCommandHandler(IEmploymentAssignmentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(AssignPositionCommand request, CancellationToken cancellationToken)
    {
        var id = new EmploymentAssignmentId(Guid.NewGuid());
        var assignmentResult = EmploymentAssignment.Create(
            id, request.TenantId, request.EmploymentId, request.PositionId, request.DepartmentId,
            request.BusinessUnitId, request.CostCenterId, request.WorkLocationId, request.LegalEntityId,
            request.WorkArrangement, request.ReportingManagerEmploymentId, request.EffectiveStartDate,
            request.PositionIsActive, _timeProvider.GetUtcNow());
        if (assignmentResult.IsFailure)
        {
            return Result.Failure<Guid>(assignmentResult.Error);
        }

        await _repository.AddAsync(assignmentResult.Value, cancellationToken).ConfigureAwait(false);
        return Result.Success(assignmentResult.Value.Id.Value);
    }
}

/// <summary>
/// Base shape shared by Transfer/Promote/Demote -- each fixes
/// <see cref="MovementType"/> to match its own name; see that type's own remarks
/// for why the classification is caller-supplied.
/// </summary>
public abstract record ChangePositionCommandBase(
    Guid EmploymentAssignmentId,
    Guid TenantId,
    Guid NewPositionId,
    Guid? NewDepartmentId,
    Guid? NewBusinessUnitId,
    Guid? NewCostCenterId,
    Guid? NewWorkLocationId,
    Guid? NewLegalEntityId,
    WorkArrangement NewWorkArrangement,
    DateOnly EffectiveDate,
    string? ApprovalReference,
    bool PositionIsActive) : ICommand<Result>;

public sealed record TransferEmploymentCommand(
    Guid EmploymentAssignmentId,
    Guid TenantId,
    Guid NewPositionId,
    Guid? NewDepartmentId,
    Guid? NewBusinessUnitId,
    Guid? NewCostCenterId,
    Guid? NewWorkLocationId,
    Guid? NewLegalEntityId,
    WorkArrangement NewWorkArrangement,
    DateOnly EffectiveDate,
    string? ApprovalReference,
    bool PositionIsActive)
    : ChangePositionCommandBase(
        EmploymentAssignmentId, TenantId, NewPositionId, NewDepartmentId, NewBusinessUnitId, NewCostCenterId,
        NewWorkLocationId, NewLegalEntityId, NewWorkArrangement, EffectiveDate, ApprovalReference, PositionIsActive);

public sealed record PromoteEmploymentCommand(
    Guid EmploymentAssignmentId,
    Guid TenantId,
    Guid NewPositionId,
    Guid? NewDepartmentId,
    Guid? NewBusinessUnitId,
    Guid? NewCostCenterId,
    Guid? NewWorkLocationId,
    Guid? NewLegalEntityId,
    WorkArrangement NewWorkArrangement,
    DateOnly EffectiveDate,
    string? ApprovalReference,
    bool PositionIsActive)
    : ChangePositionCommandBase(
        EmploymentAssignmentId, TenantId, NewPositionId, NewDepartmentId, NewBusinessUnitId, NewCostCenterId,
        NewWorkLocationId, NewLegalEntityId, NewWorkArrangement, EffectiveDate, ApprovalReference, PositionIsActive);

public sealed record DemoteEmploymentCommand(
    Guid EmploymentAssignmentId,
    Guid TenantId,
    Guid NewPositionId,
    Guid? NewDepartmentId,
    Guid? NewBusinessUnitId,
    Guid? NewCostCenterId,
    Guid? NewWorkLocationId,
    Guid? NewLegalEntityId,
    WorkArrangement NewWorkArrangement,
    DateOnly EffectiveDate,
    string? ApprovalReference,
    bool PositionIsActive)
    : ChangePositionCommandBase(
        EmploymentAssignmentId, TenantId, NewPositionId, NewDepartmentId, NewBusinessUnitId, NewCostCenterId,
        NewWorkLocationId, NewLegalEntityId, NewWorkArrangement, EffectiveDate, ApprovalReference, PositionIsActive);

internal sealed class TransferEmploymentCommandHandler : IRequestHandler<TransferEmploymentCommand, Result>
{
    private readonly IEmploymentAssignmentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public TransferEmploymentCommandHandler(IEmploymentAssignmentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public Task<Result> Handle(TransferEmploymentCommand request, CancellationToken cancellationToken) =>
        ChangePositionCommandHandler.HandleAsync(_repository, request, MovementType.Lateral, _timeProvider, cancellationToken);
}

internal sealed class PromoteEmploymentCommandHandler : IRequestHandler<PromoteEmploymentCommand, Result>
{
    private readonly IEmploymentAssignmentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public PromoteEmploymentCommandHandler(IEmploymentAssignmentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public Task<Result> Handle(PromoteEmploymentCommand request, CancellationToken cancellationToken) =>
        ChangePositionCommandHandler.HandleAsync(_repository, request, MovementType.Promotion, _timeProvider, cancellationToken);
}

internal sealed class DemoteEmploymentCommandHandler : IRequestHandler<DemoteEmploymentCommand, Result>
{
    private readonly IEmploymentAssignmentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public DemoteEmploymentCommandHandler(IEmploymentAssignmentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public Task<Result> Handle(DemoteEmploymentCommand request, CancellationToken cancellationToken) =>
        ChangePositionCommandHandler.HandleAsync(_repository, request, MovementType.Demotion, _timeProvider, cancellationToken);
}

/// <summary>Shared handling logic for Transfer/Promote/Demote, avoiding three copies of the same orchestration.</summary>
internal static class ChangePositionCommandHandler
{
    public static async Task<Result> HandleAsync(
        IEmploymentAssignmentRepository repository, ChangePositionCommandBase request, MovementType movementType,
        TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        var assignmentResult = await EmploymentLookup.LoadAssignmentForTenantAsync(
            repository, request.EmploymentAssignmentId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (assignmentResult.IsFailure)
        {
            return Result.Failure(assignmentResult.Error);
        }

        return assignmentResult.Value.ChangePosition(
            request.NewPositionId, request.NewDepartmentId, request.NewBusinessUnitId, request.NewCostCenterId,
            request.NewWorkLocationId, request.NewLegalEntityId, request.NewWorkArrangement, movementType,
            request.EffectiveDate, request.ApprovalReference, request.PositionIsActive, timeProvider.GetUtcNow());
    }
}

public sealed record ChangeReportingManagerCommand(
    Guid EmploymentAssignmentId,
    Guid TenantId,
    Guid NewReportingManagerEmploymentId,
    DateOnly EffectiveDate) : ICommand<Result>;

internal sealed class ChangeReportingManagerCommandHandler : IRequestHandler<ChangeReportingManagerCommand, Result>
{
    private readonly IEmploymentAssignmentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ChangeReportingManagerCommandHandler(IEmploymentAssignmentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ChangeReportingManagerCommand request, CancellationToken cancellationToken)
    {
        var assignmentResult = await EmploymentLookup.LoadAssignmentForTenantAsync(
            _repository, request.EmploymentAssignmentId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (assignmentResult.IsFailure)
        {
            return Result.Failure(assignmentResult.Error);
        }

        var wouldCreateCircularReporting = await _repository.WouldCreateCircularReportingAsync(
            request.TenantId, assignmentResult.Value.EmploymentId, request.NewReportingManagerEmploymentId,
            cancellationToken).ConfigureAwait(false);

        return assignmentResult.Value.ChangeReportingManager(
            request.NewReportingManagerEmploymentId, wouldCreateCircularReporting, request.EffectiveDate,
            _timeProvider.GetUtcNow());
    }
}

public sealed record EndAssignmentCommand(Guid EmploymentAssignmentId, Guid TenantId, DateOnly EffectiveDate) : ICommand<Result>;

internal sealed class EndAssignmentCommandHandler : IRequestHandler<EndAssignmentCommand, Result>
{
    private readonly IEmploymentAssignmentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public EndAssignmentCommandHandler(IEmploymentAssignmentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(EndAssignmentCommand request, CancellationToken cancellationToken)
    {
        var assignmentResult = await EmploymentLookup.LoadAssignmentForTenantAsync(
            _repository, request.EmploymentAssignmentId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return assignmentResult.IsFailure
            ? Result.Failure(assignmentResult.Error)
            : assignmentResult.Value.End(request.EffectiveDate, _timeProvider.GetUtcNow());
    }
}
