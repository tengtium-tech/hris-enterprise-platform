using System.Diagnostics.CodeAnalysis;
using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// Aggregate Root representing the contractual relationship between an individual
/// and the organization. Source: docs/04-modules/employment/domain/aggregates.md and
/// entities.md, both of which name this the module's own primary Aggregate Root;
/// ADR-0008's own canonical workforce domain model.
///
/// This Aggregate owns three independent axes, never a single combined status field
/// (employment-status.md, ADR-0008): <see cref="EmploymentType"/> (contractual
/// classification), <see cref="LifecycleStage"/> (the relationship's own
/// progression), and <see cref="OperationalStatus"/> (whether work is currently
/// being performed). It also owns <see cref="EmploymentCategory"/> (the Employment
/// Level dimension only -- Work Arrangement belongs to
/// <see cref="EmploymentAssignment"/>, per value-objects.md) and an effective-dated
/// <see cref="CompensationRecord"/> history, added to this Aggregate's own "owns"
/// list during the `compensation` module's build (EMP-007, see aggregates.md's own
/// note).
///
/// <see cref="EmployeeId"/> is a plain caller-supplied <see cref="Guid"/>, never a
/// compile-time reference to the Employee module's own type -- this platform's own
/// standing "reference by identifier, never by instance" rule, and doubly true here
/// since the Employee module does not exist in code yet (Phase 2 Sprint 4). EMP-002's
/// "cannot be created without... an approved Position assignment" and this
/// Aggregate's own <see cref="Activate"/> therefore accept caller-computed booleans
/// the Application-layer command handler derives by querying
/// <c>IEmploymentContractRepository</c> and <c>IEmploymentAssignmentRepository</c>
/// before calling this method -- the identical cross-aggregate-fact pattern
/// Position module's own <c>AssignReportingPosition</c> already establishes, since
/// EmploymentContract and EmploymentAssignment are separate Aggregate Roots this
/// Aggregate never reaches into directly (aggregates.md: "no direct modification of
/// another Aggregate").
///
/// "Employment" as both the module's own root namespace segment and this class's own
/// name reproduces the exact CS0118 name-resolution collision found while building
/// Organization and Position (see feedback memory
/// <c>feedback-module-namespace-collision</c>): any reference to this type from
/// outside its own Domain namespace must be fully qualified as
/// <c>Hris.Modules.Employment.Domain.Employment</c>, never the short
/// <c>Domain.Employment</c> partial form -- including inside the test project.
/// </summary>
[SuppressMessage(
    "Naming",
    "CA1724:Type names should not match namespaces",
    Justification = "\"Employment\" is this module's own binding ubiquitous-language term "
        + "(docs/04-modules/employment/) -- the identical precedent Hris.Modules.Organization.Domain.Organization "
        + "and Hris.Modules.Position.Domain.Position already set. Renaming the Aggregate Root to avoid a namespace "
        + "collision would depart from the documented business vocabulary for no benefit.")]
public sealed class Employment : AggregateRoot<EmploymentId>
{
    private readonly List<EmploymentStatusChange> _statusChanges = [];
    private readonly List<ProbationRecord> _probationRecords = [];
    private readonly List<CompensationRecord> _compensationRecords = [];

    public Guid TenantId { get; }

    public Guid EmployeeId { get; }

    public EmploymentNumber Number { get; }

    public EmploymentType EmploymentType { get; private set; }

    public EmploymentCategory Category { get; private set; }

    public EmploymentLifecycleStage LifecycleStage { get; private set; }

    public OperationalStatus OperationalStatus { get; private set; }

    public bool IsPrimary { get; private set; }

    public Guid? PriorEmploymentId { get; }

    public SeparationRecord? SeparationRecord { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public IReadOnlyList<EmploymentStatusChange> StatusChanges => _statusChanges.AsReadOnly();

    public IReadOnlyList<ProbationRecord> ProbationRecords => _probationRecords.AsReadOnly();

    public IReadOnlyList<CompensationRecord> CompensationRecords => _compensationRecords.AsReadOnly();

    private Employment(
        EmploymentId id, Guid tenantId, Guid employeeId, EmploymentNumber number, EmploymentType employmentType,
        EmploymentCategory category, bool isPrimary, Guid? priorEmploymentId, DateTimeOffset createdAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        Number = number;
        EmploymentType = employmentType;
        Category = category;
        LifecycleStage = EmploymentLifecycleStage.Draft;
        OperationalStatus = OperationalStatus.Active;
        IsPrimary = isPrimary;
        PriorEmploymentId = priorEmploymentId;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>
    /// Creates a new Employment in Draft stage. <paramref name="concurrentEmploymentEnabled"/>
    /// and <paramref name="employeeHasActivePrimaryEmployment"/> are computed by the
    /// calling Application-layer command handler against <c>IEmploymentRepository</c>
    /// before this method is called, enforcing EMP-004/CONC-001. When
    /// <paramref name="isPrimary"/> is <see langword="false"/>,
    /// <paramref name="primaryEmploymentId"/> must be supplied and is carried only in
    /// the raised <see cref="ConcurrentEmploymentCreated"/> event's payload.
    /// </summary>
    public static Result<Employment> Create(
        EmploymentId id, Guid tenantId, Guid employeeId, string? number, string? employmentType, string? category,
        bool isPrimary, Guid? primaryEmploymentId, Guid? priorEmploymentId,
        bool concurrentEmploymentEnabled, bool employeeHasActivePrimaryEmployment, DateTimeOffset nowUtc)
    {
        var numberResult = EmploymentNumber.Create(number);
        if (numberResult.IsFailure)
        {
            return Result.Failure<Employment>(numberResult.Error);
        }

        var typeResult = EmploymentType.Create(employmentType);
        if (typeResult.IsFailure)
        {
            return Result.Failure<Employment>(typeResult.Error);
        }

        var categoryResult = EmploymentCategory.Create(category);
        if (categoryResult.IsFailure)
        {
            return Result.Failure<Employment>(categoryResult.Error);
        }

        if (isPrimary && employeeHasActivePrimaryEmployment && !concurrentEmploymentEnabled)
        {
            return Result.Failure<Employment>(EmploymentErrors.DuplicatePrimaryEmployment);
        }

        if (!isPrimary && !concurrentEmploymentEnabled)
        {
            return Result.Failure<Employment>(EmploymentErrors.ConcurrentEmploymentNotEnabled);
        }

        var employment = new Employment(
            id, tenantId, employeeId, numberResult.Value, typeResult.Value, categoryResult.Value, isPrimary,
            priorEmploymentId, nowUtc);
        employment.AddDomainEvent(new EmploymentCreated(
            Guid.NewGuid(), nowUtc, id, tenantId, employeeId, numberResult.Value.Value, typeResult.Value.Value,
            categoryResult.Value.Value));

        if (!isPrimary && primaryEmploymentId.HasValue)
        {
            employment.AddDomainEvent(new ConcurrentEmploymentCreated(
                Guid.NewGuid(), nowUtc, id, employeeId, primaryEmploymentId.Value));
        }

        if (priorEmploymentId.HasValue)
        {
            employment.AddDomainEvent(new EmployeeRehired(Guid.NewGuid(), nowUtc, id, employeeId, priorEmploymentId.Value, DateOnly.FromDateTime(nowUtc.UtcDateTime)));
        }

        return Result.Success(employment);
    }

    /// <summary>
    /// Activates a Draft Employment. <paramref name="hasValidContract"/> and
    /// <paramref name="hasValidAssignment"/> are computed by the calling
    /// Application-layer command handler (EMP-003).
    /// </summary>
    public Result Activate(bool hasValidContract, bool hasValidAssignment, DateTimeOffset nowUtc)
    {
        if (LifecycleStage != EmploymentLifecycleStage.Draft)
        {
            return Result.Failure(EmploymentErrors.EmploymentNotDraft);
        }

        if (!hasValidContract)
        {
            return Result.Failure(EmploymentErrors.EmploymentActivationRequiresContract);
        }

        if (!hasValidAssignment)
        {
            return Result.Failure(EmploymentErrors.EmploymentActivationRequiresAssignment);
        }

        LifecycleStage = EmploymentLifecycleStage.Active;
        AddDomainEvent(new EmploymentActivated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result ChangeEmploymentType(string? newType, DateTimeOffset nowUtc)
    {
        if (LifecycleStage == EmploymentLifecycleStage.Separated)
        {
            return Result.Failure(EmploymentErrors.EmploymentSeparatedCannotBeModified);
        }

        var typeResult = EmploymentType.Create(newType);
        if (typeResult.IsFailure)
        {
            return Result.Failure(typeResult.Error);
        }

        var previous = EmploymentType.Value;
        EmploymentType = typeResult.Value;
        AddDomainEvent(new EmploymentTypeChanged(Guid.NewGuid(), nowUtc, Id, previous, typeResult.Value.Value));
        return Result.Success();
    }

    public Result ChangeEmploymentCategory(string? newCategory, DateTimeOffset nowUtc)
    {
        if (LifecycleStage == EmploymentLifecycleStage.Separated)
        {
            return Result.Failure(EmploymentErrors.EmploymentSeparatedCannotBeModified);
        }

        var categoryResult = EmploymentCategory.Create(newCategory);
        if (categoryResult.IsFailure)
        {
            return Result.Failure(categoryResult.Error);
        }

        var previous = Category.Value;
        Category = categoryResult.Value;
        AddDomainEvent(new EmploymentCategoryChanged(Guid.NewGuid(), nowUtc, Id, previous, categoryResult.Value.Value));
        return Result.Success();
    }

    /// <summary>
    /// Records a new <see cref="CompensationRecord"/>, closing the prior one without
    /// deleting or editing it (EMP-007). The only path onto compensation regardless
    /// of caller -- initial hire, an approved `compensation` module Compensation
    /// Change, or a direct correction, distinguished by <paramref name="changeSource"/>.
    /// </summary>
    public Result RecordCompensation(
        decimal amount, string? currencyCode, CompensationBasis basis, DateOnly effectiveStartDate,
        CompensationChangeSource changeSource, string? approvalReference, DateTimeOffset nowUtc)
    {
        if (LifecycleStage == EmploymentLifecycleStage.Separated)
        {
            return Result.Failure(EmploymentErrors.EmploymentSeparatedCannotBeModified);
        }

        var amountResult = CompensationAmount.Create(amount, currencyCode, basis);
        if (amountResult.IsFailure)
        {
            return Result.Failure(amountResult.Error);
        }

        var previous = _compensationRecords.SingleOrDefault(c => c.EffectiveEndDate is null);
        previous?.Close(effectiveStartDate);

        var record = new CompensationRecord(
            new CompensationRecordId(Guid.NewGuid()), effectiveStartDate, changeSource, approvalReference, nowUtc)
        {
            Amount = amountResult.Value,
        };
        _compensationRecords.Add(record);

        AddDomainEvent(new EmploymentCompensationChanged(
            Guid.NewGuid(), nowUtc, Id, EmployeeId, previous?.Id, record.Id, effectiveStartDate, changeSource));
        return Result.Success();
    }

    public Result StartProbation(DateOnly startDate, int durationDays, DateTimeOffset nowUtc)
    {
        if (LifecycleStage != EmploymentLifecycleStage.Active)
        {
            return Result.Failure(EmploymentErrors.EmploymentNotActive);
        }

        if (_probationRecords.Any(p => p.Outcome == ProbationOutcome.Pending))
        {
            return Result.Failure(EmploymentErrors.ProbationAlreadyStarted);
        }

        var durationResult = ProbationDuration.Create(durationDays);
        if (durationResult.IsFailure)
        {
            return Result.Failure(durationResult.Error);
        }

        var record = new ProbationRecord(new ProbationRecordId(Guid.NewGuid()), startDate, durationResult.Value, nowUtc);
        _probationRecords.Add(record);
        AddDomainEvent(new ProbationStarted(Guid.NewGuid(), nowUtc, Id, record.Id, startDate, record.ExpectedEvaluationDate));
        return Result.Success();
    }

    public Result ExtendProbation(int additionalDurationDays, DateTimeOffset nowUtc)
    {
        var record = _probationRecords.SingleOrDefault(p => p.Outcome == ProbationOutcome.Pending);
        if (record is null)
        {
            return Result.Failure(EmploymentErrors.ProbationNotInProgress);
        }

        var durationResult = ProbationDuration.Create(additionalDurationDays);
        if (durationResult.IsFailure)
        {
            return Result.Failure(durationResult.Error);
        }

        var extendResult = record.Extend(durationResult.Value);
        if (extendResult.IsFailure)
        {
            return extendResult;
        }

        AddDomainEvent(new ProbationExtended(Guid.NewGuid(), nowUtc, Id, record.Id, record.ExpectedEvaluationDate));
        return Result.Success();
    }

    /// <summary>
    /// Concludes a successful probation. When <paramref name="newEmploymentType"/> is
    /// supplied, the Employment Type also transitions (typically Probationary to
    /// Regular -- probation-confirmation.md step 3).
    /// </summary>
    public Result ConfirmEmployment(string? newEmploymentType, DateTimeOffset nowUtc)
    {
        var record = _probationRecords.SingleOrDefault(p => p.Outcome == ProbationOutcome.Pending);
        if (record is null)
        {
            return Result.Failure(EmploymentErrors.ProbationNotInProgress);
        }

        var confirmResult = record.Confirm();
        if (confirmResult.IsFailure)
        {
            return confirmResult;
        }

        if (!string.IsNullOrWhiteSpace(newEmploymentType))
        {
            var typeChangeResult = ChangeEmploymentType(newEmploymentType, nowUtc);
            if (typeChangeResult.IsFailure)
            {
                return typeChangeResult;
            }
        }

        AddDomainEvent(new EmploymentConfirmed(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    /// <summary>
    /// Records probation failure and separates the Employment with
    /// <see cref="SeparationType.Terminated"/> (PROB-005: failure must result in
    /// separation or an approved extension, never silent status retention).
    /// </summary>
    public Result FailProbation(DateOnly lastWorkingDate, DateOnly effectiveSeparationDate, DateTimeOffset nowUtc)
    {
        var record = _probationRecords.SingleOrDefault(p => p.Outcome == ProbationOutcome.Pending);
        if (record is null)
        {
            return Result.Failure(EmploymentErrors.ProbationNotInProgress);
        }

        var failResult = record.Fail();
        if (failResult.IsFailure)
        {
            return failResult;
        }

        AddDomainEvent(new ProbationFailed(Guid.NewGuid(), nowUtc, Id, record.Id));

        return Separate(SeparationType.Terminated, null, lastWorkingDate, effectiveSeparationDate, nowUtc);
    }

    public Result Suspend(string? reason, DateOnly effectiveDate, DateTimeOffset nowUtc)
    {
        if (LifecycleStage != EmploymentLifecycleStage.Active)
        {
            return Result.Failure(EmploymentErrors.EmploymentNotActive);
        }

        if (OperationalStatus == OperationalStatus.Suspended)
        {
            return Result.Failure(EmploymentErrors.OperationalStatusUnchanged);
        }

        var normalizedReason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        RecordStatusChange(OperationalStatus.Suspended, effectiveDate, normalizedReason, nowUtc);
        AddDomainEvent(new EmploymentSuspended(Guid.NewGuid(), nowUtc, Id, normalizedReason, effectiveDate));
        return Result.Success();
    }

    /// <summary>
    /// Returns Operational Status to Active from either <see cref="OperationalStatus.Suspended"/>
    /// or <see cref="OperationalStatus.Seconded"/>.
    /// </summary>
    public Result Reinstate(DateOnly effectiveDate, DateTimeOffset nowUtc)
    {
        if (LifecycleStage != EmploymentLifecycleStage.Active)
        {
            return Result.Failure(EmploymentErrors.EmploymentNotActive);
        }

        if (OperationalStatus == OperationalStatus.Active)
        {
            return Result.Failure(EmploymentErrors.OperationalStatusUnchanged);
        }

        RecordStatusChange(OperationalStatus.Active, effectiveDate, string.Empty, nowUtc);
        AddDomainEvent(new EmploymentReinstated(Guid.NewGuid(), nowUtc, Id, effectiveDate));
        return Result.Success();
    }

    public Result Second(DateOnly effectiveDate, DateTimeOffset nowUtc)
    {
        if (LifecycleStage != EmploymentLifecycleStage.Active)
        {
            return Result.Failure(EmploymentErrors.EmploymentNotActive);
        }

        if (OperationalStatus == OperationalStatus.Seconded)
        {
            return Result.Failure(EmploymentErrors.OperationalStatusUnchanged);
        }

        RecordStatusChange(OperationalStatus.Seconded, effectiveDate, string.Empty, nowUtc);
        AddDomainEvent(new EmploymentSeconded(Guid.NewGuid(), nowUtc, Id, effectiveDate));
        return Result.Success();
    }

    /// <summary>
    /// Reaches the terminal <see cref="EmploymentLifecycleStage.Separated"/> stage
    /// (STAT-003: only from Active). Also raises <see cref="ConcurrentEmploymentEnded"/>
    /// when this Employment is a secondary employment.
    /// </summary>
    public Result Separate(
        SeparationType separationType, string? terminationReasonText, DateOnly lastWorkingDate,
        DateOnly effectiveSeparationDate, DateTimeOffset nowUtc)
    {
        if (LifecycleStage == EmploymentLifecycleStage.Separated)
        {
            return Result.Failure(EmploymentErrors.EmploymentAlreadySeparated);
        }

        if (LifecycleStage != EmploymentLifecycleStage.Active)
        {
            return Result.Failure(EmploymentErrors.EmploymentNotActive);
        }

        TerminationReason? terminationReason = null;
        if (!string.IsNullOrWhiteSpace(terminationReasonText))
        {
            var reasonResult = TerminationReason.Create(terminationReasonText);
            if (reasonResult.IsFailure)
            {
                return Result.Failure(reasonResult.Error);
            }

            terminationReason = reasonResult.Value;
        }

        SeparationRecord = new SeparationRecord(
            new SeparationRecordId(Guid.NewGuid()), separationType, terminationReason, lastWorkingDate,
            effectiveSeparationDate, nowUtc);
        LifecycleStage = EmploymentLifecycleStage.Separated;

        AddDomainEvent(new EmploymentSeparated(Guid.NewGuid(), nowUtc, Id, separationType, effectiveSeparationDate));

        if (!IsPrimary)
        {
            AddDomainEvent(new ConcurrentEmploymentEnded(Guid.NewGuid(), nowUtc, Id, EmployeeId));
        }

        return Result.Success();
    }

    /// <summary>
    /// Designates this Employment Primary, superseding <paramref name="previousPrimaryEmploymentId"/>.
    /// The caller (Application-layer command handler) is responsible for also
    /// demoting the previous Primary Employment to secondary through its own
    /// repository-loaded instance.
    /// </summary>
    public Result MarkPrimary(Guid previousPrimaryEmploymentId, DateTimeOffset nowUtc)
    {
        if (IsPrimary)
        {
            return Result.Failure(EmploymentErrors.EmploymentAlreadyPrimary);
        }

        IsPrimary = true;
        AddDomainEvent(new PrimaryEmploymentChanged(Guid.NewGuid(), nowUtc, EmployeeId, Id, previousPrimaryEmploymentId));
        return Result.Success();
    }

    public void MarkSecondary()
    {
        IsPrimary = false;
    }

    private void RecordStatusChange(OperationalStatus newStatus, DateOnly effectiveDate, string reason, DateTimeOffset nowUtc)
    {
        var previous = OperationalStatus;
        OperationalStatus = newStatus;
        _statusChanges.Add(new EmploymentStatusChange(
            new EmploymentStatusChangeId(Guid.NewGuid()), previous, newStatus, effectiveDate, reason, nowUtc));
    }
}
