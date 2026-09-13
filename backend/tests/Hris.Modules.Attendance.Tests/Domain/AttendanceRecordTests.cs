using FluentAssertions;
using Hris.Modules.Attendance.Domain;
using Xunit;

namespace Hris.Modules.Attendance.Tests.Domain;

/// <summary>
/// The authoritative record of one employee's time for one work date: its own
/// lifecycle (Created -> Calculated -> Submitted -> Approved -> Finalized, with an
/// authorized path back to Calculated via Reject or Reopen), the immutable
/// <see cref="TimeEvent"/> set it owns, and the transactionally separate application
/// of an already-approved <see cref="AttendanceAdjustment"/> (AT-024). Every
/// documented transition gets both a success and a rejection test, per
/// docs/09-testing/unit-and-integration-testing.md §2.2.
/// </summary>
public sealed class AttendanceRecordTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _employeeId = Guid.NewGuid();

    private AttendanceRecord NewRecord() =>
        AttendanceRecord.Create(
            new AttendanceRecordId(Guid.NewGuid()), _tenantId, _employeeId, TestAttendance.Today,
            TestAttendance.NowUtc, null).Value;

    private static CalculationResult SampleCalculation() =>
        new(new CalculatedFields(8, 8, 0, 0, 0, 0, 0), [], "Test");

    // ---- Create --------------------------------------------------------

    [Fact]
    public void Create_StartsInCreatedStatus_WithDraftApprovalAndUnprocessedPayroll()
    {
        var record = NewRecord();

        record.Status.Should().Be(AttendanceStatus.Created);
        record.ApprovalStatus.Should().Be(ApprovalStatus.Draft);
        record.PayrollStatus.Should().Be(PayrollStatus.NotProcessed);
        record.TimeEvents.Should().BeEmpty();

        var raised = record.DomainEvents.OfType<AttendanceRecordCreated>().Single();
        raised.TenantId.Should().Be(_tenantId);
        raised.EmployeeId.Should().Be(_employeeId);
        raised.WorkDate.Should().Be(TestAttendance.Today);
    }

    [Fact]
    public void Create_Fails_WhenEmployeeIdIsEmpty()
    {
        var result = AttendanceRecord.Create(
            new AttendanceRecordId(Guid.NewGuid()), _tenantId, Guid.Empty, TestAttendance.Today,
            TestAttendance.NowUtc, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.EmployeeIdentifierRequired);
    }

    // ---- CaptureTimeEvent ------------------------------------------------

    [Fact]
    public void CaptureTimeEvent_Succeeds_AddsTheEventAndRaisesCaptured()
    {
        var record = NewRecord();
        var at = TestAttendance.At(9);

        var result = record.CaptureTimeEvent(
            new TimeEventId(Guid.NewGuid()), TimeEventType.ClockIn, at, AttendanceSource.MobileApplication,
            null, null, null, TestAttendance.NowUtc, Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
        record.TimeEvents.Should().ContainSingle(e => e.EventType == TimeEventType.ClockIn && e.TimestampUtc == at);

        var raised = record.DomainEvents.OfType<TimeEventCaptured>().Single();
        raised.EventType.Should().Be(TimeEventType.ClockIn);
        raised.Source.Should().Be(AttendanceSource.MobileApplication);
    }

    // Archived is checked alongside Finalized in every guard below (Status is Finalized
    // or Archived), but no method anywhere in this aggregate ever assigns Archived --
    // there is currently no reachable path into that branch to test through the public
    // API. Recorded here rather than silently tested around: worth a look at whether
    // archival is a deliberately deferred capability or a gap.
    [Fact]
    public void CaptureTimeEvent_Fails_WhenFinalized()
    {
        var record = NewRecord();
        SetStatus(record, AttendanceStatus.Finalized);

        var result = record.CaptureTimeEvent(
            new TimeEventId(Guid.NewGuid()), TimeEventType.ClockIn, TestAttendance.At(9),
            AttendanceSource.MobileApplication, null, null, null, TestAttendance.NowUtc, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.RecordAlreadyFinalized);
    }

    [Fact]
    public void CaptureTimeEvent_Fails_WhenADuplicateOfTheSameTypeAndSourceArrivesWithinOneMinute()
    {
        var record = NewRecord();
        record.CaptureTimeEvent(
            new TimeEventId(Guid.NewGuid()), TimeEventType.ClockIn, TestAttendance.At(9),
            AttendanceSource.MobileApplication, null, null, null, TestAttendance.NowUtc, null);

        var result = record.CaptureTimeEvent(
            new TimeEventId(Guid.NewGuid()), TimeEventType.ClockIn, TestAttendance.At(9, 0).AddSeconds(30),
            AttendanceSource.MobileApplication, null, null, null, TestAttendance.NowUtc, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.DuplicateTimeEvent);
        record.TimeEvents.Should().HaveCount(1, "the duplicate must not be appended");
    }

    [Fact]
    public void CaptureTimeEvent_Succeeds_WhenTheSameTypeAndSourceArrivesOutsideTheOneMinuteWindow()
    {
        var record = NewRecord();
        record.CaptureTimeEvent(
            new TimeEventId(Guid.NewGuid()), TimeEventType.BreakStart, TestAttendance.At(9),
            AttendanceSource.MobileApplication, null, null, null, TestAttendance.NowUtc, null);

        var result = record.CaptureTimeEvent(
            new TimeEventId(Guid.NewGuid()), TimeEventType.BreakStart, TestAttendance.At(9).AddMinutes(2),
            AttendanceSource.MobileApplication, null, null, null, TestAttendance.NowUtc, null);

        result.IsSuccess.Should().BeTrue();
        record.TimeEvents.Should().HaveCount(2, "two minutes apart is a second break, not a resubmit of the first");
    }

    [Fact]
    public void CaptureTimeEvent_Succeeds_WhenTheSameTimestampCarriesADifferentEventType()
    {
        var record = NewRecord();
        var at = TestAttendance.At(9);
        record.CaptureTimeEvent(
            new TimeEventId(Guid.NewGuid()), TimeEventType.ClockIn, at, AttendanceSource.MobileApplication,
            null, null, null, TestAttendance.NowUtc, null);

        var result = record.CaptureTimeEvent(
            new TimeEventId(Guid.NewGuid()), TimeEventType.BreakStart, at, AttendanceSource.MobileApplication,
            null, null, null, TestAttendance.NowUtc, null);

        result.IsSuccess.Should().BeTrue("the dedup window compares type and source together, not timestamp alone");
    }

    // ---- ApplyCalculation ------------------------------------------------

    [Fact]
    public void ApplyCalculation_SetsCalculatedFieldsAndStatus_AndRaisesCalculatedEvent()
    {
        var record = NewRecord();

        var result = record.ApplyCalculation(SampleCalculation(), TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        record.Status.Should().Be(AttendanceStatus.Calculated);
        record.Calculated.Should().NotBeNull();
        record.Calculated!.WorkingHours.Should().Be(8);

        var raised = record.DomainEvents.OfType<AttendanceRecordCalculated>().Single();
        raised.WorkingHours.Should().Be(8);
        raised.Trigger.Should().Be("Test");
    }

    [Fact]
    public void ApplyCalculation_Fails_WhenFinalized()
    {
        var record = NewRecord();
        SetStatus(record, AttendanceStatus.Finalized);

        var result = record.ApplyCalculation(SampleCalculation(), TestAttendance.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.RecordAlreadyFinalized);
    }

    [Fact]
    public void ApplyCalculation_ReplacesPriorExceptions_RatherThanAccumulatingThem()
    {
        var record = NewRecord();
        record.ApplyCalculation(new CalculationResult(new CalculatedFields(1, 1, 0, 0, 0, 0, 0), ["first run exception"], "Test"), TestAttendance.NowUtc);

        record.ApplyCalculation(new CalculationResult(new CalculatedFields(2, 2, 0, 0, 0, 0, 0), ["second run exception"], "Test"), TestAttendance.NowUtc);

        record.Exceptions.Should().ContainSingle().Which.Should().Be("second run exception");
    }

    // ---- Submit ------------------------------------------------------

    [Fact]
    public void Submit_Succeeds_FromCalculated()
    {
        var record = NewRecord();
        record.ApplyCalculation(SampleCalculation(), TestAttendance.NowUtc);
        var actorId = Guid.NewGuid();

        var result = record.Submit(actorId, TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        record.Status.Should().Be(AttendanceStatus.Submitted);
        record.ApprovalStatus.Should().Be(ApprovalStatus.Pending);
        record.DomainEvents.OfType<AttendanceRecordSubmittedForApproval>().Single().ActorId.Should().Be(actorId);
    }

    [Theory]
    [InlineData(AttendanceStatus.Created)]
    [InlineData(AttendanceStatus.Submitted)]
    [InlineData(AttendanceStatus.Approved)]
    [InlineData(AttendanceStatus.Finalized)]
    public void Submit_Fails_WhenTheRecordIsNotCalculated(AttendanceStatus sourceStatus)
    {
        var record = NewRecord();
        SetStatus(record, sourceStatus);

        var result = record.Submit(Guid.NewGuid(), TestAttendance.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.RecordNotValidated);
    }

    // ---- Approve -----------------------------------------------------

    [Fact]
    public void Approve_Succeeds_FromSubmitted()
    {
        var record = NewRecord();
        SetStatus(record, AttendanceStatus.Submitted);
        var approverId = Guid.NewGuid();
        var decision = new ApprovalDecision(approverId, ApprovalDecisionOutcome.Approved, TestAttendance.NowUtc, null, null, null);

        var result = record.Approve(approverId, decision, TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        record.Status.Should().Be(AttendanceStatus.Approved);
        record.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
        record.DomainEvents.OfType<AttendanceRecordApproved>().Single().ApproverId.Should().Be(approverId);
    }

    [Theory]
    [InlineData(AttendanceStatus.Created)]
    [InlineData(AttendanceStatus.Calculated)]
    [InlineData(AttendanceStatus.Approved)]
    [InlineData(AttendanceStatus.Finalized)]
    public void Approve_Fails_WhenTheRecordIsNotSubmitted(AttendanceStatus sourceStatus)
    {
        var record = NewRecord();
        SetStatus(record, sourceStatus);
        var decision = new ApprovalDecision(Guid.NewGuid(), ApprovalDecisionOutcome.Approved, TestAttendance.NowUtc, null, null, null);

        var result = record.Approve(Guid.NewGuid(), decision, TestAttendance.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.InvalidStateTransition);
    }

    // ---- Reject ------------------------------------------------------

    [Fact]
    public void Reject_Succeeds_FromSubmitted_AndReturnsToCalculatedForCorrection()
    {
        var record = NewRecord();
        SetStatus(record, AttendanceStatus.Submitted);
        var approverId = Guid.NewGuid();

        var result = record.Reject(approverId, "hours look wrong", TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        record.Status.Should().Be(AttendanceStatus.Calculated, "a rejected record returns for correction, it is not terminal");
        record.ApprovalStatus.Should().Be(ApprovalStatus.Rejected);
        record.DomainEvents.OfType<AttendanceRecordRejected>().Single().Reason.Should().Be("hours look wrong");
    }

    [Theory]
    [InlineData(AttendanceStatus.Created)]
    [InlineData(AttendanceStatus.Calculated)]
    [InlineData(AttendanceStatus.Approved)]
    public void Reject_Fails_WhenTheRecordIsNotSubmitted(AttendanceStatus sourceStatus)
    {
        var record = NewRecord();
        SetStatus(record, sourceStatus);

        var result = record.Reject(Guid.NewGuid(), "reason", TestAttendance.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.InvalidStateTransition);
    }

    // ---- Finalize ----------------------------------------------------

    [Fact]
    public void Finalize_Succeeds_FromApproved()
    {
        var record = NewRecord();
        SetStatus(record, AttendanceStatus.Approved);
        var actorId = Guid.NewGuid();

        var result = record.Finalize(actorId, TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        record.Status.Should().Be(AttendanceStatus.Finalized);
        record.DomainEvents.OfType<AttendanceRecordFinalized>().Single().ActorId.Should().Be(actorId);
    }

    [Fact]
    public void Finalize_IsIdempotent_WhenAlreadyFinalized()
    {
        var record = NewRecord();
        SetStatus(record, AttendanceStatus.Finalized);
        record.ClearDomainEvents();

        var result = record.Finalize(Guid.NewGuid(), TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        record.DomainEvents.Should().BeEmpty("an idempotent no-op must not re-raise the event a real transition already raised once");
    }

    [Theory]
    [InlineData(AttendanceStatus.Created)]
    [InlineData(AttendanceStatus.Calculated)]
    [InlineData(AttendanceStatus.Submitted)]
    public void Finalize_Fails_WhenTheRecordIsNotApproved(AttendanceStatus sourceStatus)
    {
        var record = NewRecord();
        SetStatus(record, sourceStatus);

        var result = record.Finalize(Guid.NewGuid(), TestAttendance.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.InvalidStateTransition);
    }

    [Fact]
    public void Finalize_Fails_WhenAnAdjustmentIsStillPending()
    {
        var record = NewRecord();
        SetStatus(record, AttendanceStatus.Approved);
        record.MarkAdjustmentSubmitted();

        var result = record.Finalize(Guid.NewGuid(), TestAttendance.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.PendingAdjustmentBlocksFinalization);
    }

    // ---- Reopen ------------------------------------------------------

    [Fact]
    public void Reopen_Succeeds_FromFinalized_AndReturnsToCalculatedWithDraftApproval()
    {
        var record = NewRecord();
        SetStatus(record, AttendanceStatus.Finalized);
        var actorId = Guid.NewGuid();

        var result = record.Reopen(actorId, "AUTH-123", "payroll correction needed", TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        record.Status.Should().Be(AttendanceStatus.Calculated);
        record.ApprovalStatus.Should().Be(ApprovalStatus.Draft);
        var raised = record.DomainEvents.OfType<AttendanceRecordReopened>().Single();
        raised.AuthorizationReference.Should().Be("AUTH-123");
        raised.ActorId.Should().Be(actorId);
    }

    [Theory]
    [InlineData(AttendanceStatus.Created)]
    [InlineData(AttendanceStatus.Calculated)]
    [InlineData(AttendanceStatus.Submitted)]
    [InlineData(AttendanceStatus.Approved)]
    public void Reopen_Fails_WhenTheRecordIsNotFinalized(AttendanceStatus sourceStatus)
    {
        var record = NewRecord();
        SetStatus(record, sourceStatus);

        var result = record.Reopen(Guid.NewGuid(), "AUTH-123", "reason", TestAttendance.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.InvalidStateTransition);
    }

    // ---- ApplyAdjustment (AT-024) ---------------------------------------

    [Fact]
    public void ApplyAdjustment_Fails_WhenFinalized()
    {
        var record = NewRecord();
        SetStatus(record, AttendanceStatus.Finalized);

        var result = record.ApplyAdjustment(new AttendanceAdjustmentId(Guid.NewGuid()), "WorkingHours", "9", TestAttendance.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.RecordAlreadyFinalized);
    }

    [Fact]
    public void ApplyAdjustment_UpdatesTheNamedCalculatedField_CaseInsensitively()
    {
        var record = NewRecord();
        record.ApplyCalculation(SampleCalculation(), TestAttendance.NowUtc);

        record.ApplyAdjustment(new AttendanceAdjustmentId(Guid.NewGuid()), "workinghours", "9.5", TestAttendance.NowUtc);

        record.Calculated!.WorkingHours.Should().Be(9.5);
    }

    [Fact]
    public void ApplyAdjustment_LeavesCalculatedUnchanged_WhenNoCalculationHasEverRun()
    {
        var record = NewRecord();

        var result = record.ApplyAdjustment(new AttendanceAdjustmentId(Guid.NewGuid()), "WorkingHours", "9.5", TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue("recording the applied adjustment does not itself require a prior calculation");
        record.Calculated.Should().BeNull();
    }

    [Fact]
    public void ApplyAdjustment_LeavesCalculatedUnchanged_WhenTheRequestedValueIsNotNumeric()
    {
        var record = NewRecord();
        record.ApplyCalculation(SampleCalculation(), TestAttendance.NowUtc);

        record.ApplyAdjustment(new AttendanceAdjustmentId(Guid.NewGuid()), "WorkingHours", "not-a-number", TestAttendance.NowUtc);

        record.Calculated!.WorkingHours.Should().Be(8, "an unparseable requested value must not silently zero the field");
    }

    [Fact]
    public void ApplyAdjustment_LeavesCalculatedUnchanged_WhenTheFieldNameIsUnrecognized()
    {
        var record = NewRecord();
        record.ApplyCalculation(SampleCalculation(), TestAttendance.NowUtc);

        record.ApplyAdjustment(new AttendanceAdjustmentId(Guid.NewGuid()), "SomeUnknownField", "9.5", TestAttendance.NowUtc);

        record.Calculated!.WorkingHours.Should().Be(8);
    }

    [Fact]
    public void ApplyAdjustment_AddsToAppliedAdjustments_AndRaisesAppliedEvent()
    {
        var record = NewRecord();
        var adjustmentId = new AttendanceAdjustmentId(Guid.NewGuid());

        record.ApplyAdjustment(adjustmentId, "WorkingHours", "9.5", TestAttendance.NowUtc);

        record.AppliedAdjustments.Should().ContainSingle(a => a.AdjustmentId == adjustmentId && a.RequestedValue == "9.5");
        record.DomainEvents.OfType<AttendanceAdjustmentApplied>().Single().AttendanceAdjustmentId.Should().Be(adjustmentId);
    }

    [Fact]
    public void ApplyAdjustment_DecrementsPendingAdjustmentCount_ButNeverBelowZero()
    {
        var record = NewRecord();
        record.MarkAdjustmentSubmitted();
        record.MarkAdjustmentSubmitted();
        record.PendingAdjustmentCount.Should().Be(2);

        record.ApplyAdjustment(new AttendanceAdjustmentId(Guid.NewGuid()), "WorkingHours", "9", TestAttendance.NowUtc);
        record.PendingAdjustmentCount.Should().Be(1);

        record.ApplyAdjustment(new AttendanceAdjustmentId(Guid.NewGuid()), "WorkingHours", "9", TestAttendance.NowUtc);
        record.PendingAdjustmentCount.Should().Be(0);

        record.ApplyAdjustment(new AttendanceAdjustmentId(Guid.NewGuid()), "WorkingHours", "9", TestAttendance.NowUtc);
        record.PendingAdjustmentCount.Should().Be(0, "the count must never go negative");
    }

    // ---- Internal helpers (NoteResolvedReferences / MarkAdjustment*) ------

    [Fact]
    public void NoteResolvedReferences_StoresTheShiftAndHolidayCalendarIdentifiers()
    {
        var record = NewRecord();
        var shiftId = Guid.NewGuid();
        var calendarId = Guid.NewGuid();

        record.NoteResolvedReferences(shiftId, calendarId);

        record.WorkShiftId.Should().Be(shiftId);
        record.HolidayCalendarId.Should().Be(calendarId);
    }

    [Fact]
    public void MarkAdjustmentResolved_DecrementsCount_ButNeverBelowZero()
    {
        var record = NewRecord();

        record.MarkAdjustmentResolved();

        record.PendingAdjustmentCount.Should().Be(0);
    }

    /// <summary>Drives a freshly created record to the given status via its own public transitions.</summary>
    private static void SetStatus(AttendanceRecord record, AttendanceStatus status)
    {
        if (status == AttendanceStatus.Created)
        {
            return;
        }

        record.ApplyCalculation(SampleCalculation(), TestAttendance.NowUtc);
        if (status == AttendanceStatus.Calculated)
        {
            return;
        }

        record.Submit(Guid.NewGuid(), TestAttendance.NowUtc);
        if (status == AttendanceStatus.Submitted)
        {
            return;
        }

        record.Approve(
            Guid.NewGuid(), new ApprovalDecision(Guid.NewGuid(), ApprovalDecisionOutcome.Approved, TestAttendance.NowUtc, null, null, null),
            TestAttendance.NowUtc);
        if (status == AttendanceStatus.Approved)
        {
            return;
        }

        record.Finalize(Guid.NewGuid(), TestAttendance.NowUtc);
    }
}
