using Hris.Application.Abstractions;
using Hris.Modules.Timekeeping.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Timekeeping.Application.Commands;

/// <summary>Authors a new shift in Draft. Source: application/commands.md.</summary>
public sealed record DefineWorkShiftCommand(
    Guid TenantId,
    string? Code,
    string? Name,
    ShiftTimingInput? Timing,
    bool IsOvernight,
    WorkDateAnchorPoint? AnchorPoint,
    string? AnchorDescription,
    IReadOnlyList<ShiftPeriodInput>? SplitPeriods,
    IReadOnlyList<BreakRuleInput>? BreakRules,
    bool OvertimeEligible,
    bool NightDifferentialEligible,
    bool HazardEligible,
    bool HolidayPremiumEligible,
    DateOnly EffectiveFrom,
    Guid ActingUser) : ICommand<Result<Guid>>;

internal sealed class DefineWorkShiftCommandHandler : IRequestHandler<DefineWorkShiftCommand, Result<Guid>>
{
    private readonly IWorkShiftRepository _repository;
    private readonly TimeProvider _timeProvider;

    public DefineWorkShiftCommandHandler(IWorkShiftRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(DefineWorkShiftCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        if (request.Timing is null)
        {
            return Result.Failure<Guid>(TimekeepingErrors.FixedTimingRequiresWindow);
        }

        var timingResult = request.Timing.ToTiming();
        if (timingResult.IsFailure)
        {
            return Result.Failure<Guid>(timingResult.Error);
        }

        var codeInUse = !string.IsNullOrWhiteSpace(request.Code)
            && await _repository.CodeExistsInTenantAsync(
                request.TenantId, request.Code.Trim(), null, cancellationToken).ConfigureAwait(false);

        var anchorRule = request.AnchorPoint is null
            ? null
            : WorkDateAnchorRule.Create(request.AnchorPoint.Value, request.AnchorDescription);

        var result = WorkShift.Create(
            new WorkShiftId(Guid.NewGuid()), request.TenantId, request.Code, request.Name, timingResult.Value,
            request.IsOvernight, anchorRule, request.SplitPeriods?.Select(p => p.ToShiftPeriod()).ToList(),
            request.BreakRules?.Select(b => b.ToBreakRule()).ToList(), request.OvertimeEligible,
            PremiumEligibilityFlags.Create(
                request.NightDifferentialEligible, request.HazardEligible, request.HolidayPremiumEligible),
            request.EffectiveFrom, codeInUse, request.ActingUser, _timeProvider.GetUtcNow());

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _repository.AddAsync(result.Value, cancellationToken).ConfigureAwait(false);
        return Result.Success(result.Value.Id.Value);
    }
}

public sealed record PublishWorkShiftCommand(Guid TenantId, Guid WorkShiftId, Guid ActingUser) : ICommand<Result>;

internal sealed class PublishWorkShiftCommandHandler : IRequestHandler<PublishWorkShiftCommand, Result>
{
    private readonly IWorkShiftRepository _repository;
    private readonly TimeProvider _timeProvider;

    public PublishWorkShiftCommandHandler(IWorkShiftRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(PublishWorkShiftCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var shiftResult = await TimekeepingLookup
            .LoadShiftForTenantAsync(_repository, request.WorkShiftId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return shiftResult.IsFailure
            ? Result.Failure(shiftResult.Error)
            : shiftResult.Value.Publish(request.ActingUser, _timeProvider.GetUtcNow());
    }
}

/// <summary>
/// Produces the next version of a shift (TK-001). Raises the narrower
/// overtime-eligibility event alongside the supersede event when that flag moved,
/// since a consumer that only cares about overtime behaviour should not have to diff
/// two complete shift definitions to notice.
/// </summary>
public sealed record ReviseWorkShiftCommand(
    Guid TenantId,
    Guid WorkShiftId,
    string? Name,
    ShiftTimingInput? Timing,
    bool IsOvernight,
    WorkDateAnchorPoint? AnchorPoint,
    string? AnchorDescription,
    IReadOnlyList<ShiftPeriodInput>? SplitPeriods,
    IReadOnlyList<BreakRuleInput>? BreakRules,
    bool OvertimeEligible,
    bool NightDifferentialEligible,
    bool HazardEligible,
    bool HolidayPremiumEligible,
    DateOnly NewEffectiveFrom,
    Guid ActingUser) : ICommand<Result<Guid>>;

internal sealed class ReviseWorkShiftCommandHandler : IRequestHandler<ReviseWorkShiftCommand, Result<Guid>>
{
    private readonly IWorkShiftRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ReviseWorkShiftCommandHandler(IWorkShiftRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(ReviseWorkShiftCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var shiftResult = await TimekeepingLookup
            .LoadShiftForTenantAsync(_repository, request.WorkShiftId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (shiftResult.IsFailure)
        {
            return Result.Failure<Guid>(shiftResult.Error);
        }

        if (request.Timing is null)
        {
            return Result.Failure<Guid>(TimekeepingErrors.FixedTimingRequiresWindow);
        }

        var timingResult = request.Timing.ToTiming();
        if (timingResult.IsFailure)
        {
            return Result.Failure<Guid>(timingResult.Error);
        }

        var anchorRule = request.AnchorPoint is null
            ? null
            : WorkDateAnchorRule.Create(request.AnchorPoint.Value, request.AnchorDescription);

        var nextResult = shiftResult.Value.Supersede(
            new WorkShiftId(Guid.NewGuid()), request.Name, timingResult.Value, request.IsOvernight, anchorRule,
            request.SplitPeriods?.Select(p => p.ToShiftPeriod()).ToList(),
            request.BreakRules?.Select(b => b.ToBreakRule()).ToList(), request.OvertimeEligible,
            PremiumEligibilityFlags.Create(
                request.NightDifferentialEligible, request.HazardEligible, request.HolidayPremiumEligible),
            request.NewEffectiveFrom, request.ActingUser, _timeProvider.GetUtcNow());

        if (nextResult.IsFailure)
        {
            return Result.Failure<Guid>(nextResult.Error);
        }

        await _repository.AddAsync(nextResult.Value, cancellationToken).ConfigureAwait(false);
        return Result.Success(nextResult.Value.Id.Value);
    }
}

public sealed record RetireWorkShiftCommand(
    Guid TenantId, Guid WorkShiftId, DateOnly EffectiveFrom, Guid ActingUser) : ICommand<Result>;

internal sealed class RetireWorkShiftCommandHandler : IRequestHandler<RetireWorkShiftCommand, Result>
{
    private readonly IWorkShiftRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RetireWorkShiftCommandHandler(IWorkShiftRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RetireWorkShiftCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var shiftResult = await TimekeepingLookup
            .LoadShiftForTenantAsync(_repository, request.WorkShiftId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return shiftResult.IsFailure
            ? Result.Failure(shiftResult.Error)
            : shiftResult.Value.Retire(request.ActingUser, request.EffectiveFrom, _timeProvider.GetUtcNow());
    }
}
