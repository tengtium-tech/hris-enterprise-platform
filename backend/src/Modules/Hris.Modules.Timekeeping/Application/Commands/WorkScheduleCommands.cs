using Hris.Application.Abstractions;
using Hris.Modules.Timekeeping.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Timekeeping.Application.Commands;

/// <summary>Authors a new schedule in Draft. Source: application/commands.md.</summary>
public sealed record DefineWorkScheduleCommand(
    Guid TenantId,
    string? Name,
    string? Description,
    IReadOnlyList<DayOfWeek>? WorkingDays,
    TimeOnly? StandardHoursStart,
    TimeOnly? StandardHoursEnd,
    IReadOnlyList<BreakRuleInput>? BreakPeriods,
    DateOnly EffectiveFrom,
    Guid ActingUser) : ICommand<Result<Guid>>;

internal sealed class DefineWorkScheduleCommandHandler : IRequestHandler<DefineWorkScheduleCommand, Result<Guid>>
{
    private readonly IWorkScheduleRepository _repository;
    private readonly TimeProvider _timeProvider;

    public DefineWorkScheduleCommandHandler(IWorkScheduleRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(DefineWorkScheduleCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        TimeWindow? standardHours = null;
        if (request.StandardHoursStart is not null && request.StandardHoursEnd is not null)
        {
            var windowResult = TimeWindow.Create(request.StandardHoursStart.Value, request.StandardHoursEnd.Value);
            if (windowResult.IsFailure)
            {
                return Result.Failure<Guid>(windowResult.Error);
            }

            standardHours = windowResult.Value;
        }

        var breaks = request.BreakPeriods?.Select(input => input.ToBreakRule()).ToList();

        var result = WorkSchedule.Create(
            new WorkScheduleId(Guid.NewGuid()), request.TenantId, request.Name, request.Description,
            request.WorkingDays, standardHours, breaks, request.EffectiveFrom, request.ActingUser,
            _timeProvider.GetUtcNow());

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _repository.AddAsync(result.Value, cancellationToken).ConfigureAwait(false);
        return Result.Success(result.Value.Id.Value);
    }
}

/// <summary>Makes a draft schedule Active.</summary>
public sealed record PublishWorkScheduleCommand(Guid TenantId, Guid WorkScheduleId, Guid ActingUser) : ICommand<Result>;

internal sealed class PublishWorkScheduleCommandHandler : IRequestHandler<PublishWorkScheduleCommand, Result>
{
    private readonly IWorkScheduleRepository _repository;
    private readonly TimeProvider _timeProvider;

    public PublishWorkScheduleCommandHandler(IWorkScheduleRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(PublishWorkScheduleCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var scheduleResult = await TimekeepingLookup
            .LoadScheduleForTenantAsync(_repository, request.WorkScheduleId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return scheduleResult.IsFailure
            ? Result.Failure(scheduleResult.Error)
            : scheduleResult.Value.Publish(request.ActingUser, _timeProvider.GetUtcNow());
    }
}

/// <summary>
/// Produces the next version of a schedule (TK-001). There is deliberately no
/// command that edits a Published or Superseded version in place: revision creates a
/// new version, and the one it replaces stays readable exactly as it was, because
/// attendance evaluations and payroll runs already depended on it.
/// </summary>
public sealed record ReviseWorkScheduleCommand(
    Guid TenantId,
    Guid WorkScheduleId,
    IReadOnlyList<DayOfWeek>? WorkingDays,
    TimeOnly? StandardHoursStart,
    TimeOnly? StandardHoursEnd,
    IReadOnlyList<BreakRuleInput>? BreakPeriods,
    DateOnly NewEffectiveFrom,
    Guid ActingUser) : ICommand<Result<Guid>>;

internal sealed class ReviseWorkScheduleCommandHandler : IRequestHandler<ReviseWorkScheduleCommand, Result<Guid>>
{
    private readonly IWorkScheduleRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ReviseWorkScheduleCommandHandler(IWorkScheduleRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(ReviseWorkScheduleCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var scheduleResult = await TimekeepingLookup
            .LoadScheduleForTenantAsync(_repository, request.WorkScheduleId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (scheduleResult.IsFailure)
        {
            return Result.Failure<Guid>(scheduleResult.Error);
        }

        TimeWindow? standardHours = null;
        if (request.StandardHoursStart is not null && request.StandardHoursEnd is not null)
        {
            var windowResult = TimeWindow.Create(request.StandardHoursStart.Value, request.StandardHoursEnd.Value);
            if (windowResult.IsFailure)
            {
                return Result.Failure<Guid>(windowResult.Error);
            }

            standardHours = windowResult.Value;
        }

        var breaks = request.BreakPeriods?.Select(input => input.ToBreakRule()).ToList();

        var nextResult = scheduleResult.Value.Supersede(
            new WorkScheduleId(Guid.NewGuid()), request.WorkingDays, standardHours, breaks, request.NewEffectiveFrom,
            request.ActingUser, _timeProvider.GetUtcNow());

        if (nextResult.IsFailure)
        {
            return Result.Failure<Guid>(nextResult.Error);
        }

        await _repository.AddAsync(nextResult.Value, cancellationToken).ConfigureAwait(false);
        return Result.Success(nextResult.Value.Id.Value);
    }
}

/// <summary>Withdraws a schedule from new assignment. Existing assignments are unaffected.</summary>
public sealed record RetireWorkScheduleCommand(
    Guid TenantId, Guid WorkScheduleId, DateOnly EffectiveFrom, Guid ActingUser) : ICommand<Result>;

internal sealed class RetireWorkScheduleCommandHandler : IRequestHandler<RetireWorkScheduleCommand, Result>
{
    private readonly IWorkScheduleRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RetireWorkScheduleCommandHandler(IWorkScheduleRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RetireWorkScheduleCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var scheduleResult = await TimekeepingLookup
            .LoadScheduleForTenantAsync(_repository, request.WorkScheduleId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return scheduleResult.IsFailure
            ? Result.Failure(scheduleResult.Error)
            : scheduleResult.Value.Retire(request.ActingUser, request.EffectiveFrom, _timeProvider.GetUtcNow());
    }
}

/// <summary>Assigns a schedule to an organizational unit for an effective period (TK-011).</summary>
public sealed record AssignWorkScheduleCommand(
    Guid TenantId,
    Guid WorkScheduleId,
    OrganizationalAssignmentLevel TargetLevel,
    string? TargetId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    Guid ActingUser) : ICommand<Result<Guid>>;

internal sealed class AssignWorkScheduleCommandHandler : IRequestHandler<AssignWorkScheduleCommand, Result<Guid>>
{
    private readonly IWorkScheduleRepository _repository;
    private readonly TimeProvider _timeProvider;

    public AssignWorkScheduleCommandHandler(IWorkScheduleRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(AssignWorkScheduleCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var scheduleResult = await TimekeepingLookup
            .LoadScheduleForTenantAsync(_repository, request.WorkScheduleId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (scheduleResult.IsFailure)
        {
            return Result.Failure<Guid>(scheduleResult.Error);
        }

        var result = scheduleResult.Value.AssignTo(
            new ScheduleAssignmentId(Guid.NewGuid()), request.TargetLevel, request.TargetId, request.EffectiveFrom,
            request.EffectiveTo, request.ActingUser, _timeProvider.GetUtcNow());

        return result.IsFailure ? Result.Failure<Guid>(result.Error) : Result.Success(result.Value.Value);
    }
}

/// <summary>
/// End-dates a schedule assignment (TK-012). Deliberately not a delete: what
/// schedule applied to an organizational unit during a past period must stay
/// answerable.
/// </summary>
public sealed record UnassignWorkScheduleCommand(
    Guid TenantId, Guid WorkScheduleId, Guid ScheduleAssignmentId, DateOnly EffectiveTo, Guid ActingUser)
    : ICommand<Result>;

internal sealed class UnassignWorkScheduleCommandHandler : IRequestHandler<UnassignWorkScheduleCommand, Result>
{
    private readonly IWorkScheduleRepository _repository;
    private readonly TimeProvider _timeProvider;

    public UnassignWorkScheduleCommandHandler(IWorkScheduleRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(UnassignWorkScheduleCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var scheduleResult = await TimekeepingLookup
            .LoadScheduleForTenantAsync(_repository, request.WorkScheduleId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return scheduleResult.IsFailure
            ? Result.Failure(scheduleResult.Error)
            : scheduleResult.Value.Unassign(
                new ScheduleAssignmentId(request.ScheduleAssignmentId), request.EffectiveTo, request.ActingUser,
                _timeProvider.GetUtcNow());
    }
}
