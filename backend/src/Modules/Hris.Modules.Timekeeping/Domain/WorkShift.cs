using Hris.SharedKernel;

namespace Hris.Modules.Timekeeping.Domain;

/// <summary>
/// Aggregate Root representing the concrete daily working-time definition,
/// independent of any one employee. Source:
/// docs/04-modules/timekeeping/domain/aggregates.md and work-shifts.md.
///
/// The load-bearing decision here is that a shift states its own overnight anchor
/// rule rather than leaving <c>attendance</c> to infer it. aggregates.md is explicit
/// about why deferring it would be a mistake: the rule for which work date a 22:00
/// to 06:00 shift belongs to is a property of the definition, fixed once and read by
/// every consumer, not a judgment made fresh each time an event is processed. If it
/// lived downstream, an attendance run and a later payroll reconciliation reading
/// the same shift independently could each derive a different anchor date for the
/// same timestamp.
///
/// <see cref="OvertimeEligible"/> is a property of the version, uniform across every
/// assignment and date it governs (TK-022). A shift that permits overtime for one
/// assigned employee and not another is not a distinction this module models; that
/// is two different shifts.
/// </summary>
public sealed class WorkShift : AggregateRoot<WorkShiftId>
{
    public Guid TenantId { get; }

    public Guid LineageId { get; }

    public ShiftCode Code { get; internal set; } = null!;

    public string Name { get; private set; } = null!;

    public ShiftTiming Timing { get; internal set; } = null!;

    public bool IsOvernight { get; private set; }

    /// <summary>Required when <see cref="IsOvernight"/>, prohibited otherwise (TK-020).</summary>
    public WorkDateAnchorRule? AnchorRule { get; internal set; }

    /// <summary>Empty for a non-split shift. Persisted as a JSON column; no per-item identity.</summary>
    public IReadOnlyList<ShiftPeriod> SplitPeriods { get; internal set; } = [];

    public IReadOnlyList<BreakRule> BreakRules { get; internal set; } = [];

    public bool OvertimeEligible { get; private set; }

    public PremiumEligibilityFlags PremiumEligibility { get; internal set; } = null!;

    public int Version { get; }

    public DateOnly EffectiveFrom { get; }

    public DateOnly? EffectiveTo { get; private set; }

    public WorkShiftStatus Status { get; private set; }

    public Guid CreatedBy { get; }

    public DateTimeOffset CreatedOn { get; }

    private WorkShift(
        WorkShiftId id, Guid tenantId, Guid lineageId, int version, DateOnly effectiveFrom, Guid createdBy,
        DateTimeOffset createdOn)
        : base(id)
    {
        TenantId = tenantId;
        LineageId = lineageId;
        Version = version;
        EffectiveFrom = effectiveFrom;
        Status = WorkShiftStatus.Draft;
        CreatedBy = createdBy;
        CreatedOn = createdOn;
    }

    /// <summary>
    /// <paramref name="codeAlreadyUsedInTenant"/> is computed by the calling handler
    /// against the tenant's other shifts, a cross-aggregate fact this type cannot see
    /// about itself.
    /// </summary>
    public static Result<WorkShift> Create(
        WorkShiftId id, Guid tenantId, string? code, string? name, ShiftTiming? timing, bool isOvernight,
        WorkDateAnchorRule? anchorRule, IReadOnlyList<ShiftPeriod>? splitPeriods, IReadOnlyList<BreakRule>? breakRules,
        bool overtimeEligible, PremiumEligibilityFlags? premiumEligibility, DateOnly effectiveFrom,
        bool codeAlreadyUsedInTenant, Guid createdBy, DateTimeOffset createdOn) =>
        Build(id, tenantId, id.Value, 1, code, name, timing, isOvernight, anchorRule, splitPeriods, breakRules,
            overtimeEligible, premiumEligibility, effectiveFrom, codeAlreadyUsedInTenant, createdBy, createdOn);

    private static Result<WorkShift> Build(
        WorkShiftId id, Guid tenantId, Guid lineageId, int version, string? code, string? name, ShiftTiming? timing,
        bool isOvernight, WorkDateAnchorRule? anchorRule, IReadOnlyList<ShiftPeriod>? splitPeriods,
        IReadOnlyList<BreakRule>? breakRules, bool overtimeEligible, PremiumEligibilityFlags? premiumEligibility,
        DateOnly effectiveFrom, bool codeAlreadyUsedInTenant, Guid createdBy, DateTimeOffset createdOn)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<WorkShift>(TimekeepingErrors.WorkShiftNameRequired);
        }

        var codeResult = ShiftCode.Create(code);
        if (codeResult.IsFailure)
        {
            return Result.Failure<WorkShift>(codeResult.Error);
        }

        if (codeAlreadyUsedInTenant)
        {
            return Result.Failure<WorkShift>(TimekeepingErrors.ShiftCodeNotUniqueWithinTenant);
        }

        if (timing is null)
        {
            return Result.Failure<WorkShift>(TimekeepingErrors.FixedTimingRequiresWindow);
        }

        // TK-020, both halves. An overnight shift without an anchor rule leaves the
        // work date to be inferred; a non-overnight shift carrying one invites a
        // consumer to apply it to a case that cannot occur.
        if (isOvernight && anchorRule is null)
        {
            return Result.Failure<WorkShift>(TimekeepingErrors.OvernightShiftRequiresAnchorRule);
        }

        if (!isOvernight && anchorRule is not null)
        {
            return Result.Failure<WorkShift>(TimekeepingErrors.AnchorRuleProhibitedForNonOvernightShift);
        }

        var periods = splitPeriods?.OrderBy(period => period.Sequence).ToList() ?? [];
        var periodsResult = ValidateSplitPeriods(periods);
        if (periodsResult.IsFailure)
        {
            return Result.Failure<WorkShift>(periodsResult.Error);
        }

        var breaks = breakRules?.ToList() ?? [];
        if (breaks.Any(rule => rule.Mandatory && rule.Duration <= TimeSpan.Zero))
        {
            return Result.Failure<WorkShift>(TimekeepingErrors.MandatoryBreakRequiresPositiveDuration);
        }

        var shift = new WorkShift(id, tenantId, lineageId, version, effectiveFrom, createdBy, createdOn)
        {
            Code = codeResult.Value,
            Name = name.Trim(),
            Timing = timing,
            IsOvernight = isOvernight,
            AnchorRule = anchorRule,
            SplitPeriods = periods,
            BreakRules = breaks,
            OvertimeEligible = overtimeEligible,
            PremiumEligibility = premiumEligibility ?? PremiumEligibilityFlags.None,
        };

        return Result.Success(shift);
    }

    private static Result ValidateSplitPeriods(List<ShiftPeriod> periods)
    {
        if (periods.Count == 0)
        {
            return Result.Success();
        }

        if (periods.Count == 1)
        {
            return Result.Failure(TimekeepingErrors.SplitShiftRequiresAtLeastTwoPeriods);
        }

        if (periods.Any(period => period.End <= period.Start))
        {
            return Result.Failure(TimekeepingErrors.ShiftPeriodEndNotAfterStart);
        }

        for (var i = 0; i < periods.Count; i++)
        {
            for (var j = i + 1; j < periods.Count; j++)
            {
                if (periods[i].OverlapsWith(periods[j]))
                {
                    return Result.Failure(TimekeepingErrors.SplitShiftPeriodsOverlap);
                }
            }
        }

        return Result.Success();
    }

    public Result Publish(Guid publishedBy, DateTimeOffset nowUtc)
    {
        if (Status == WorkShiftStatus.Superseded)
        {
            return Result.Failure(TimekeepingErrors.SupersededVersionCannotBeModified);
        }

        if (Status != WorkShiftStatus.Draft)
        {
            return Result.Failure(TimekeepingErrors.VersionNotActive);
        }

        Status = WorkShiftStatus.Active;
        AddDomainEvent(new WorkShiftPublished(Guid.NewGuid(), nowUtc, Id, TenantId, Version, EffectiveFrom, publishedBy));
        return Result.Success();
    }

    /// <summary>
    /// Produces the next version, leaving this one byte-identical (TK-001).
    ///
    /// Raises <see cref="WorkShiftOvertimeEligibilityChanged"/> in addition to
    /// <see cref="WorkShiftSuperseded"/> when that one flag moved, per
    /// domain-events.md: a consumer that only cares about overtime behaviour should
    /// not have to diff two complete shift definitions to notice.
    /// </summary>
    public Result<WorkShift> Supersede(
        WorkShiftId newId, string? name, ShiftTiming? timing, bool isOvernight, WorkDateAnchorRule? anchorRule,
        IReadOnlyList<ShiftPeriod>? splitPeriods, IReadOnlyList<BreakRule>? breakRules, bool overtimeEligible,
        PremiumEligibilityFlags? premiumEligibility, DateOnly newEffectiveFrom, Guid changedBy, DateTimeOffset nowUtc)
    {
        if (Status != WorkShiftStatus.Active)
        {
            return Result.Failure<WorkShift>(TimekeepingErrors.VersionNotActive);
        }

        if (newEffectiveFrom <= EffectiveFrom)
        {
            return Result.Failure<WorkShift>(TimekeepingErrors.EffectiveFromNotAfterCurrentVersion);
        }

        var nextResult = Build(
            newId, TenantId, LineageId, Version + 1, Code.Value, name, timing, isOvernight, anchorRule, splitPeriods,
            breakRules, overtimeEligible, premiumEligibility, newEffectiveFrom, false, changedBy, nowUtc);
        if (nextResult.IsFailure)
        {
            return nextResult;
        }

        var next = nextResult.Value;

        Status = WorkShiftStatus.Superseded;
        EffectiveTo = newEffectiveFrom.AddDays(-1);

        next.AddDomainEvent(new WorkShiftSuperseded(
            Guid.NewGuid(), nowUtc, next.Id, TenantId, Version, next.Version, newEffectiveFrom));

        if (OvertimeEligible != overtimeEligible)
        {
            next.AddDomainEvent(new WorkShiftOvertimeEligibilityChanged(
                Guid.NewGuid(), nowUtc, next.Id, TenantId, OvertimeEligible, overtimeEligible, newEffectiveFrom,
                changedBy));
        }

        return Result.Success(next);
    }

    public Result Retire(Guid retiredBy, DateOnly effectiveFrom, DateTimeOffset nowUtc)
    {
        if (Status == WorkShiftStatus.Retired)
        {
            return Result.Success();
        }

        if (Status != WorkShiftStatus.Active)
        {
            return Result.Failure(TimekeepingErrors.VersionNotActive);
        }

        Status = WorkShiftStatus.Retired;
        AddDomainEvent(new WorkShiftRetired(Guid.NewGuid(), nowUtc, Id, TenantId, effectiveFrom, retiredBy));
        return Result.Success();
    }

    public bool IsEffectiveOn(DateOnly date) =>
        EffectiveFrom <= date && (EffectiveTo is null || date <= EffectiveTo.Value);

    /// <summary>
    /// The single work date this shift's hours belong to, given the date it started
    /// on. One place computes it; every consumer reads the same answer, which is the
    /// point of holding the anchor rule here rather than in <c>attendance</c>. A
    /// non-overnight shift always anchors to its own start date.
    /// </summary>
    public DateOnly ResolveWorkDate(DateOnly startDate) =>
        IsOvernight && AnchorRule is not null ? AnchorRule.ResolveWorkDate(startDate) : startDate;
}
