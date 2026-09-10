using Hris.Application.Abstractions;
using Hris.Modules.Timekeeping.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Timekeeping.Application.Commands;

/// <summary>
/// Binds a shift to an employee or organizational unit. TK-031's overlap check is
/// performed here against the employee's other individual-level assignments, since
/// no single aggregate instance can see them.
///
/// There is deliberately no command that changes the shift or target of an existing
/// Active assignment in place: changing what an employee is expected to work is a
/// new assignment superseding the old one, the same discipline revision applies at
/// the schedule and shift level.
/// </summary>
public sealed record CreateShiftAssignmentCommand(
    Guid TenantId,
    AssignmentTargetType TargetType,
    string? TargetId,
    OrganizationalAssignmentLevel TargetLevel,
    Guid WorkShiftId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsTemporary,
    Guid? RotationCycleReference,
    Guid? ActingUser) : ICommand<Result<Guid>>;

internal sealed class CreateShiftAssignmentCommandHandler : IRequestHandler<CreateShiftAssignmentCommand, Result<Guid>>
{
    private readonly IShiftAssignmentRepository _repository;
    private readonly IWorkShiftRepository _shiftRepository;
    private readonly TimeProvider _timeProvider;

    public CreateShiftAssignmentCommandHandler(
        IShiftAssignmentRepository repository, IWorkShiftRepository shiftRepository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _shiftRepository = Guard.AgainstNull(shiftRepository, nameof(shiftRepository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(CreateShiftAssignmentCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        // Every assignment references a shift that exists — the first invariant in
        // aggregates.md's ShiftAssignment list, and one only the handler can check.
        var shiftResult = await TimekeepingLookup
            .LoadShiftForTenantAsync(_shiftRepository, request.WorkShiftId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (shiftResult.IsFailure)
        {
            return Result.Failure<Guid>(shiftResult.Error);
        }

        var overlaps = false;
        if (request.TargetLevel == OrganizationalAssignmentLevel.IndividualEmployee
            && !string.IsNullOrWhiteSpace(request.TargetId))
        {
            var existing = await _repository
                .ListIndividualByEmployeeAsync(request.TenantId, request.TargetId.Trim(), cancellationToken)
                .ConfigureAwait(false);

            overlaps = existing.Any(assignment =>
                assignment.Status is ShiftAssignmentStatus.Scheduled or ShiftAssignmentStatus.Active
                && assignment.OverlapsPeriod(request.EffectiveFrom, request.EffectiveTo));
        }

        var result = ShiftAssignment.Create(
            new ShiftAssignmentId(Guid.NewGuid()), request.TenantId, request.TargetType, request.TargetId,
            request.TargetLevel, new WorkShiftId(request.WorkShiftId), request.EffectiveFrom, request.EffectiveTo,
            request.IsTemporary,
            request.RotationCycleReference is null ? null : new RotationCycleId(request.RotationCycleReference.Value),
            overlaps, request.ActingUser, _timeProvider.GetUtcNow());

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _repository.AddAsync(result.Value, cancellationToken).ConfigureAwait(false);
        return Result.Success(result.Value.Id.Value);
    }
}

public sealed record CancelShiftAssignmentCommand(
    Guid TenantId, Guid ShiftAssignmentId, string? Reason, DateOnly EffectiveDate, Guid ActingUser) : ICommand<Result>;

internal sealed class CancelShiftAssignmentCommandHandler : IRequestHandler<CancelShiftAssignmentCommand, Result>
{
    private readonly IShiftAssignmentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CancelShiftAssignmentCommandHandler(IShiftAssignmentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(CancelShiftAssignmentCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var assignmentResult = await TimekeepingLookup
            .LoadAssignmentForTenantAsync(_repository, request.ShiftAssignmentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return assignmentResult.IsFailure
            ? Result.Failure(assignmentResult.Error)
            : assignmentResult.Value.Cancel(
                request.Reason, request.ActingUser, request.EffectiveDate, _timeProvider.GetUtcNow());
    }
}

/// <summary>Step one of the four-command swap flow (commands.md, "Why Swap Is Four Commands, Not One").</summary>
public sealed record ProposeShiftSwapCommand(
    Guid TenantId,
    Guid PrimaryAssignmentId,
    Guid SecondaryAssignmentId,
    DateOnly ProposedEffectiveFrom,
    Guid ActingUser) : ICommand<Result>;

internal sealed class ProposeShiftSwapCommandHandler : IRequestHandler<ProposeShiftSwapCommand, Result>
{
    private readonly IShiftAssignmentRepository _repository;

    public ProposeShiftSwapCommandHandler(IShiftAssignmentRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result> Handle(ProposeShiftSwapCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var primaryResult = await TimekeepingLookup
            .LoadAssignmentForTenantAsync(_repository, request.PrimaryAssignmentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (primaryResult.IsFailure)
        {
            return Result.Failure(primaryResult.Error);
        }

        var secondaryResult = await TimekeepingLookup
            .LoadAssignmentForTenantAsync(_repository, request.SecondaryAssignmentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (secondaryResult.IsFailure)
        {
            return Result.Failure(secondaryResult.Error);
        }

        return primaryResult.Value.ProposeSwap(
            secondaryResult.Value.Id, primaryResult.Value.TargetId, secondaryResult.Value.TargetId,
            request.ProposedEffectiveFrom, request.ActingUser);
    }
}

/// <summary>Step two. Consent is matched to the named employee, never counted.</summary>
public sealed record ConsentToShiftSwapCommand(
    Guid TenantId, Guid PrimaryAssignmentId, string? ConsentingEmployeeId) : ICommand<Result>;

internal sealed class ConsentToShiftSwapCommandHandler : IRequestHandler<ConsentToShiftSwapCommand, Result>
{
    private readonly IShiftAssignmentRepository _repository;

    public ConsentToShiftSwapCommandHandler(IShiftAssignmentRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result> Handle(ConsentToShiftSwapCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var assignmentResult = await TimekeepingLookup
            .LoadAssignmentForTenantAsync(_repository, request.PrimaryAssignmentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return assignmentResult.IsFailure
            ? Result.Failure(assignmentResult.Error)
            : assignmentResult.Value.ConsentToSwap(request.ConsentingEmployeeId);
    }
}

/// <summary>
/// Step three, optional. <see cref="ActorHoldsOverrideAuthority"/> is caller-supplied
/// because whether an actor holds it is an Authorization Framework decision this
/// module does not own.
/// </summary>
public sealed record OverrideShiftSwapCommand(
    Guid TenantId, Guid PrimaryAssignmentId, Guid ActingUser, string? Reason, bool ActorHoldsOverrideAuthority)
    : ICommand<Result>;

internal sealed class OverrideShiftSwapCommandHandler : IRequestHandler<OverrideShiftSwapCommand, Result>
{
    private readonly IShiftAssignmentRepository _repository;

    public OverrideShiftSwapCommandHandler(IShiftAssignmentRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result> Handle(OverrideShiftSwapCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var assignmentResult = await TimekeepingLookup
            .LoadAssignmentForTenantAsync(_repository, request.PrimaryAssignmentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return assignmentResult.IsFailure
            ? Result.Failure(assignmentResult.Error)
            : assignmentResult.Value.OverrideSwapConsent(
                request.ActingUser, request.Reason, request.ActorHoldsOverrideAuthority);
    }
}

/// <summary>
/// Step four. Moves both sides and raises exactly one
/// <see cref="ShiftAssignmentSwapped"/> carrying both, per domain-events.md — a
/// consumer needs to see the swap as one occurrence, and two independent
/// reassignment events would not say the two are related.
///
/// Coordinated here rather than inside either aggregate, because neither half can
/// see the other. Per aggregates.md, a swap is two aggregate operations, not one
/// cross-aggregate transaction.
/// </summary>
public sealed record ActivateShiftSwapCommand(Guid TenantId, Guid PrimaryAssignmentId) : ICommand<Result>;

internal sealed class ActivateShiftSwapCommandHandler : IRequestHandler<ActivateShiftSwapCommand, Result>
{
    private readonly IShiftAssignmentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ActivateShiftSwapCommandHandler(IShiftAssignmentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ActivateShiftSwapCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var primaryResult = await TimekeepingLookup
            .LoadAssignmentForTenantAsync(_repository, request.PrimaryAssignmentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (primaryResult.IsFailure)
        {
            return Result.Failure(primaryResult.Error);
        }

        var primary = primaryResult.Value;
        var pending = primary.PendingSwap;
        if (pending is null)
        {
            return Result.Failure(TimekeepingErrors.SwapRequiresBothPartiesConsent);
        }

        var secondaryResult = await TimekeepingLookup
            .LoadAssignmentForTenantAsync(
                _repository, pending.CounterpartAssignmentId.Value, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (secondaryResult.IsFailure)
        {
            return Result.Failure(secondaryResult.Error);
        }

        var secondary = secondaryResult.Value;

        var primaryActivation = primary.ActivateSwap(secondary.Id);
        if (primaryActivation.IsFailure)
        {
            return primaryActivation;
        }

        var secondaryActivation = secondary.ActivateSwap(primary.Id);
        if (secondaryActivation.IsFailure)
        {
            return secondaryActivation;
        }

        primary.RecordSwapCompleted(secondary, pending, _timeProvider.GetUtcNow());

        return Result.Success();
    }
}

/// <summary>
/// Expires assignments whose end date has passed. TK-032 and TK-051: automatic, with
/// no actor recorded, because none acted. A temporary assignment that had to be
/// manually ended would eventually be forgotten and quietly become permanent.
/// </summary>
public sealed record ExpireDueShiftAssignmentsCommand(DateOnly AsOfDate) : ICommand<Result<int>>;

internal sealed class ExpireDueShiftAssignmentsCommandHandler
    : IRequestHandler<ExpireDueShiftAssignmentsCommand, Result<int>>
{
    private readonly IShiftAssignmentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ExpireDueShiftAssignmentsCommandHandler(IShiftAssignmentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<int>> Handle(ExpireDueShiftAssignmentsCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var due = await _repository.ListExpirableAsync(request.AsOfDate, cancellationToken).ConfigureAwait(false);
        var nowUtc = _timeProvider.GetUtcNow();
        var expired = 0;

        foreach (var assignment in due)
        {
            if (assignment.Expire(nowUtc).IsSuccess)
            {
                expired++;
            }
        }

        return Result.Success(expired);
    }
}
