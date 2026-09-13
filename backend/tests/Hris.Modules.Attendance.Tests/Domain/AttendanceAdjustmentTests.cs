using FluentAssertions;
using Hris.Modules.Attendance.Domain;
using Xunit;

namespace Hris.Modules.Attendance.Tests.Domain;

/// <summary>
/// AT-020 through AT-024: one correction request and its seven-state review lifecycle,
/// with the two-phase approve/apply split (AT-024) that is this aggregate's own most
/// intricate invariant. Every documented transition gets two tests — the permitted
/// transition succeeds, the impermissible one is rejected — per
/// docs/09-testing/unit-and-integration-testing.md §2.2.
///
/// Several tests assert the exact data carried on a raised domain event rather than
/// only the resulting status. That is deliberate, not incidental thoroughness: two real
/// defects were found and fixed in this exact area before this test project existed —
/// <see cref="AttendanceAdjustment.Reject"/> once silently omitted the
/// <c>AttendanceRecordId</c> argument its own event required, and
/// <c>AttendanceAdjustmentCancelled</c> once had no <c>AttendanceRecordId</c> field at
/// all despite both its raising code and its subscriber already assuming one existed.
/// A test that only checks <c>Status</c> after the transition would not have caught
/// either defect.
/// </summary>
public sealed class AttendanceAdjustmentTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly AttendanceRecordId _recordId = new(Guid.NewGuid());

    private AttendanceAdjustment NewAdjustment() =>
        TestAttendance.Adjustment(tenantId: _tenantId, attendanceRecordId: _recordId);

    // ---- Create --------------------------------------------------------

    [Fact]
    public void Create_StartsInDraft_AndRaisesSubmittedEvent()
    {
        var adjustment = NewAdjustment();

        adjustment.Status.Should().Be(AdjustmentStatus.Draft);

        var raised = adjustment.DomainEvents.OfType<AttendanceAdjustmentSubmitted>().Single();
        raised.TenantId.Should().Be(_tenantId);
        raised.AttendanceAdjustmentId.Should().Be(adjustment.Id);
        raised.AttendanceRecordId.Should().Be(_recordId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Fails_WhenFieldIsMissing(string field)
    {
        var result = AttendanceAdjustment.Create(
            new AttendanceAdjustmentId(Guid.NewGuid()), _tenantId, _recordId, TestAttendance.Today,
            field, "18:00", "18:30", AdjustmentCategory.ForgotClockOut, "reason", null,
            Guid.NewGuid(), TestAttendance.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.OriginalValueSnapshotRequired);
    }

    [Fact]
    public void Create_SnapshotsOriginalValue_SeparatelyFromRequestedValue()
    {
        // AT-020: the original value is a snapshot at submission, never a live reference,
        // and never the same slot as the requested change.
        var adjustment = NewAdjustment();

        adjustment.OriginalValue.Should().Be("18:00");
        adjustment.RequestedValue.Should().Be("18:30");
    }

    // ---- Submit ----------------------------------------------------------

    [Fact]
    public void Submit_TransitionsToSubmitted_FromDraft()
    {
        var adjustment = NewAdjustment();

        var result = adjustment.Submit(Guid.NewGuid(), TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        adjustment.Status.Should().Be(AdjustmentStatus.Submitted);
    }

    [Fact]
    public void Submit_Fails_WhenAlreadySubmitted()
    {
        var adjustment = NewAdjustment();
        adjustment.Submit(Guid.NewGuid(), TestAttendance.NowUtc);

        var result = adjustment.Submit(Guid.NewGuid(), TestAttendance.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.AdjustmentNotInReviewableState);
    }

    // ---- Review ------------------------------------------------------

    [Fact]
    public void Review_TransitionsToUnderReview_FromSubmitted_AndRaisesEvent()
    {
        var adjustment = NewAdjustment();
        adjustment.Submit(Guid.NewGuid(), TestAttendance.NowUtc);
        var reviewerId = Guid.NewGuid();

        var result = adjustment.Review(reviewerId, "looks reasonable", TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        adjustment.Status.Should().Be(AdjustmentStatus.UnderReview);
        adjustment.ReviewerId.Should().Be(reviewerId);
        adjustment.ReviewNotes.Should().Be("looks reasonable");

        var raised = adjustment.DomainEvents.OfType<AttendanceAdjustmentReviewed>().Single();
        raised.ReviewerId.Should().Be(reviewerId);
        raised.AttendanceAdjustmentId.Should().Be(adjustment.Id);
    }

    [Fact]
    public void Review_Fails_WhenStillDraft()
    {
        var adjustment = NewAdjustment();

        var result = adjustment.Review(Guid.NewGuid(), "notes", TestAttendance.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.AdjustmentNotInReviewableState);
    }

    // ---- Approve -----------------------------------------------------

    public static TheoryData<Action<AttendanceAdjustment>> ApprovableSourceStates() => new()
    {
        static adjustment => { },
        static adjustment => adjustment.Submit(Guid.NewGuid(), TestAttendance.NowUtc),
        static adjustment =>
        {
            adjustment.Submit(Guid.NewGuid(), TestAttendance.NowUtc);
            adjustment.Review(Guid.NewGuid(), "n", TestAttendance.NowUtc);
        },
    };

    [Theory]
    [MemberData(nameof(ApprovableSourceStates))]
    public void Approve_Succeeds_FromDraftSubmittedOrUnderReview(Action<AttendanceAdjustment> moveToSourceState)
    {
        ArgumentNullException.ThrowIfNull(moveToSourceState);

        var adjustment = NewAdjustment();
        moveToSourceState(adjustment);

        var result = adjustment.Approve(
            Guid.NewGuid(), new ApprovalDecision(Guid.NewGuid(), ApprovalDecisionOutcome.Approved, TestAttendance.NowUtc, null, null, null),
            TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        adjustment.Status.Should().Be(AdjustmentStatus.Approved);
    }

    [Fact]
    public void Approve_RaisesApprovedEvent_CarryingTheAttendanceRecordIdFieldAndRequestedValue()
    {
        // Regression: the sibling Reject() once omitted AttendanceRecordId from the
        // analogous event entirely. Approve() has always included it, and this pins
        // that down explicitly rather than relying on it staying that way by accident.
        var adjustment = NewAdjustment();
        var approverId = Guid.NewGuid();

        adjustment.Approve(
            approverId, new ApprovalDecision(approverId, ApprovalDecisionOutcome.Approved, TestAttendance.NowUtc, null, null, null),
            TestAttendance.NowUtc);

        var raised = adjustment.DomainEvents.OfType<AttendanceAdjustmentApproved>().Single();
        raised.AttendanceRecordId.Should().Be(_recordId);
        raised.Field.Should().Be("ClockOut");
        raised.RequestedValue.Should().Be("18:30");
        raised.ApproverId.Should().Be(approverId);
    }

    [Fact]
    public void Approve_IsIdempotent_WhenAlreadyApproved()
    {
        var adjustment = NewAdjustment();
        var decision = new ApprovalDecision(Guid.NewGuid(), ApprovalDecisionOutcome.Approved, TestAttendance.NowUtc, null, null, null);
        adjustment.Approve(Guid.NewGuid(), decision, TestAttendance.NowUtc);
        adjustment.ClearDomainEvents();

        var result = adjustment.Approve(Guid.NewGuid(), decision, TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        adjustment.DomainEvents.OfType<AttendanceAdjustmentApproved>().Should().BeEmpty(
            "an idempotent no-op must not re-raise the event a real transition already raised once");
    }

    [Fact]
    public void Approve_IsIdempotent_WhenAlreadyApplied()
    {
        var adjustment = NewAdjustment();
        var decision = new ApprovalDecision(Guid.NewGuid(), ApprovalDecisionOutcome.Approved, TestAttendance.NowUtc, null, null, null);
        adjustment.Approve(Guid.NewGuid(), decision, TestAttendance.NowUtc);
        adjustment.MarkApplied();
        adjustment.ClearDomainEvents();

        var result = adjustment.Approve(Guid.NewGuid(), decision, TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        adjustment.Status.Should().Be(AdjustmentStatus.Applied, "approving an already-applied adjustment must not regress its status");
        adjustment.DomainEvents.Should().BeEmpty();
    }

    [Theory]
    [InlineData(AdjustmentStatus.Rejected)]
    [InlineData(AdjustmentStatus.Cancelled)]
    public void Approve_Fails_WhenRejectedOrCancelled(AdjustmentStatus terminalStatus)
    {
        var adjustment = NewAdjustment();
        MoveTo(adjustment, terminalStatus);

        var result = adjustment.Approve(
            Guid.NewGuid(), new ApprovalDecision(Guid.NewGuid(), ApprovalDecisionOutcome.Approved, TestAttendance.NowUtc, null, null, null),
            TestAttendance.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.AdjustmentNotInReviewableState);
    }

    // ---- Reject ------------------------------------------------------

    [Fact]
    public void Reject_Succeeds_FromApproved()
    {
        // A still-pending approval (not yet applied) may still be rejected -- approval
        // does not itself commit anything against the record (AT-024).
        var adjustment = NewAdjustment();
        adjustment.Approve(
            Guid.NewGuid(), new ApprovalDecision(Guid.NewGuid(), ApprovalDecisionOutcome.Approved, TestAttendance.NowUtc, null, null, null),
            TestAttendance.NowUtc);

        var result = adjustment.Reject(Guid.NewGuid(), "changed mind", TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        adjustment.Status.Should().Be(AdjustmentStatus.Rejected);
    }

    [Fact]
    public void Reject_RaisesRejectedEvent_CarryingTheAttendanceRecordId()
    {
        // Regression: this event's raising code once omitted AttendanceRecordId entirely,
        // shifting every subsequent positional argument by one.
        var adjustment = NewAdjustment();
        var approverId = Guid.NewGuid();

        adjustment.Reject(approverId, "not justified", TestAttendance.NowUtc);

        var raised = adjustment.DomainEvents.OfType<AttendanceAdjustmentRejected>().Single();
        raised.AttendanceRecordId.Should().Be(_recordId);
        raised.ApproverId.Should().Be(approverId);
        raised.Reason.Should().Be("not justified");
    }

    [Theory]
    [InlineData(AdjustmentStatus.Rejected)]
    [InlineData(AdjustmentStatus.Cancelled)]
    [InlineData(AdjustmentStatus.Applied)]
    public void Reject_Fails_WhenAlreadyRejectedCancelledOrApplied(AdjustmentStatus terminalStatus)
    {
        var adjustment = NewAdjustment();
        MoveTo(adjustment, terminalStatus);

        var result = adjustment.Reject(Guid.NewGuid(), "reason", TestAttendance.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.AdjustmentNotInReviewableState);
    }

    // ---- Cancel ------------------------------------------------------

    [Theory]
    [InlineData(AdjustmentStatus.Draft)]
    [InlineData(AdjustmentStatus.Submitted)]
    [InlineData(AdjustmentStatus.UnderReview)]
    [InlineData(AdjustmentStatus.Approved)]
    [InlineData(AdjustmentStatus.Rejected)]
    public void Cancel_Succeeds_FromEveryNonTerminalState(AdjustmentStatus sourceStatus)
    {
        var adjustment = NewAdjustment();
        MoveTo(adjustment, sourceStatus);

        var result = adjustment.Cancel(Guid.NewGuid(), "withdrawn", TestAttendance.NowUtc);

        result.IsSuccess.Should().BeTrue();
        adjustment.Status.Should().Be(AdjustmentStatus.Cancelled);
    }

    [Fact]
    public void Cancel_RaisesCancelledEvent_CarryingTheAttendanceRecordId()
    {
        // Regression: AttendanceAdjustmentCancelled once had no AttendanceRecordId field
        // at all, even though this raising code (and the event's own subscriber) already
        // assumed one existed -- the record's own build error is what first surfaced it.
        var adjustment = NewAdjustment();
        var actorId = Guid.NewGuid();

        adjustment.Cancel(actorId, "no longer needed", TestAttendance.NowUtc);

        var raised = adjustment.DomainEvents.OfType<AttendanceAdjustmentCancelled>().Single();
        raised.AttendanceRecordId.Should().Be(_recordId);
        raised.ActorId.Should().Be(actorId);
    }

    [Fact]
    public void Cancel_Fails_WhenApplied()
    {
        var adjustment = NewAdjustment();
        MoveTo(adjustment, AdjustmentStatus.Applied);

        var result = adjustment.Cancel(Guid.NewGuid(), "reason", TestAttendance.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.AdjustmentNotInReviewableState);
    }

    [Fact]
    public void Cancel_Fails_WhenAlreadyCancelled()
    {
        var adjustment = NewAdjustment();
        adjustment.Cancel(Guid.NewGuid(), "first cancel", TestAttendance.NowUtc);

        var result = adjustment.Cancel(Guid.NewGuid(), "second cancel", TestAttendance.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.AdjustmentNotInReviewableState);
    }

    // ---- MarkApplied ---------------------------------------------------

    [Fact]
    public void MarkApplied_Succeeds_WhenApproved()
    {
        var adjustment = NewAdjustment();
        MoveTo(adjustment, AdjustmentStatus.Approved);

        var result = adjustment.MarkApplied();

        result.IsSuccess.Should().BeTrue();
        adjustment.Status.Should().Be(AdjustmentStatus.Applied);
    }

    [Fact]
    public void MarkApplied_IsIdempotent_WhenAlreadyApplied()
    {
        var adjustment = NewAdjustment();
        MoveTo(adjustment, AdjustmentStatus.Applied);

        var result = adjustment.MarkApplied();

        result.IsSuccess.Should().BeTrue();
        adjustment.Status.Should().Be(AdjustmentStatus.Applied);
    }

    [Theory]
    [InlineData(AdjustmentStatus.Draft)]
    [InlineData(AdjustmentStatus.Submitted)]
    [InlineData(AdjustmentStatus.UnderReview)]
    [InlineData(AdjustmentStatus.Rejected)]
    [InlineData(AdjustmentStatus.Cancelled)]
    public void MarkApplied_Fails_WhenNotApproved(AdjustmentStatus sourceStatus)
    {
        var adjustment = NewAdjustment();
        MoveTo(adjustment, sourceStatus);

        var result = adjustment.MarkApplied();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.AdjustmentNotApproved);
    }

    /// <summary>Drives a freshly created (Draft) adjustment to the given status via its own public transitions.</summary>
    private static void MoveTo(AttendanceAdjustment adjustment, AdjustmentStatus status)
    {
        if (status == AdjustmentStatus.Draft)
        {
            return;
        }

        adjustment.Submit(Guid.NewGuid(), TestAttendance.NowUtc);
        if (status == AdjustmentStatus.Submitted)
        {
            return;
        }

        adjustment.Review(Guid.NewGuid(), "n", TestAttendance.NowUtc);
        if (status == AdjustmentStatus.UnderReview)
        {
            return;
        }

        if (status == AdjustmentStatus.Rejected)
        {
            adjustment.Reject(Guid.NewGuid(), "n", TestAttendance.NowUtc);
            return;
        }

        if (status == AdjustmentStatus.Cancelled)
        {
            adjustment.Cancel(Guid.NewGuid(), "n", TestAttendance.NowUtc);
            return;
        }

        adjustment.Approve(
            Guid.NewGuid(), new ApprovalDecision(Guid.NewGuid(), ApprovalDecisionOutcome.Approved, TestAttendance.NowUtc, null, null, null),
            TestAttendance.NowUtc);
        if (status == AdjustmentStatus.Approved)
        {
            return;
        }

        adjustment.MarkApplied();
    }
}
