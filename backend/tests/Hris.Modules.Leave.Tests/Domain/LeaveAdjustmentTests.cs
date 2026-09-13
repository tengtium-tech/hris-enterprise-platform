using FluentAssertions;
using Hris.Modules.Leave.Domain;
using Xunit;

namespace Hris.Modules.Leave.Tests.Domain;

/// <summary>
/// LV-050 through LV-054: the controlled, seven-state correction process, mirroring
/// <c>Hris.Modules.Attendance.Domain.AttendanceAdjustment</c>'s own lifecycle. Unlike that
/// aggregate, this module's own lifecycle diagram (leave-adjustments.md) draws Rejected as
/// a branch from Under Review only, not from Approved — this aggregate's <see cref="LeaveAdjustment.Reject"/>
/// is tested against that documented shape rather than Attendance's own broader one.
/// </summary>
public sealed class LeaveAdjustmentTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly LeaveBalanceId _leaveBalanceId = new(Guid.NewGuid());

    private LeaveAdjustment NewAdjustment() =>
        LeaveAdjustment.Create(
            new LeaveAdjustmentId(Guid.NewGuid()), _tenantId, _leaveBalanceId, 5m, 2m, "accrual correction", null,
            Guid.NewGuid(), TestLeave.NowUtc).Value;

    private static ApprovalDecision ApprovedDecision(Guid approverId) =>
        new(approverId, ApprovalDecisionOutcome.Approved, TestLeave.NowUtc, null, null, null);

    // ---- Create --------------------------------------------------------

    [Fact]
    public void Create_StartsInDraft_AndRaisesSubmittedEvent()
    {
        var adjustment = NewAdjustment();

        adjustment.Status.Should().Be(LeaveAdjustmentStatus.Draft);
        adjustment.OriginalValueSnapshot.Should().Be(5m);
        adjustment.RequestedAmount.Should().Be(2m);
        adjustment.DomainEvents.OfType<LeaveAdjustmentSubmitted>().Should().ContainSingle();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Fails_WhenReasonIsMissing(string? reason)
    {
        var result = LeaveAdjustment.Create(
            new LeaveAdjustmentId(Guid.NewGuid()), _tenantId, _leaveBalanceId, 5m, 2m, reason, null, Guid.NewGuid(), TestLeave.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.AdjustmentReasonRequired);
    }

    [Fact]
    public void Create_Fails_WhenRequestedAmountIsZero()
    {
        var result = LeaveAdjustment.Create(
            new LeaveAdjustmentId(Guid.NewGuid()), _tenantId, _leaveBalanceId, 5m, 0m, "reason", null, Guid.NewGuid(), TestLeave.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.LedgerEntryAmountMustNotBeZero);
    }

    // ---- Submit ----------------------------------------------------------

    [Fact]
    public void Submit_TransitionsToSubmitted_FromDraft()
    {
        var adjustment = NewAdjustment();

        var result = adjustment.Submit(TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        adjustment.Status.Should().Be(LeaveAdjustmentStatus.Submitted);
    }

    [Fact]
    public void Submit_Fails_WhenAlreadySubmitted()
    {
        var adjustment = NewAdjustment();
        adjustment.Submit(TestLeave.NowUtc);

        var result = adjustment.Submit(TestLeave.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.AdjustmentNotInReviewableState);
    }

    // ---- Review ------------------------------------------------------

    [Fact]
    public void Review_TransitionsToUnderReview_FromSubmitted()
    {
        var adjustment = NewAdjustment();
        adjustment.Submit(TestLeave.NowUtc);
        var reviewerId = Guid.NewGuid();

        var result = adjustment.Review(reviewerId, "looks reasonable", TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        adjustment.Status.Should().Be(LeaveAdjustmentStatus.UnderReview);
        adjustment.ReviewerId.Should().Be(reviewerId);
        adjustment.ReviewNotes.Should().Be("looks reasonable");
    }

    [Fact]
    public void Review_Fails_WhenStillDraft()
    {
        var adjustment = NewAdjustment();

        var result = adjustment.Review(Guid.NewGuid(), "notes", TestLeave.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.AdjustmentNotInReviewableState);
    }

    // ---- Approve -----------------------------------------------------

    public static TheoryData<Action<LeaveAdjustment>> ApprovableSourceStates() => new()
    {
        static adjustment => { },
        static adjustment => adjustment.Submit(TestLeave.NowUtc),
        static adjustment =>
        {
            adjustment.Submit(TestLeave.NowUtc);
            adjustment.Review(Guid.NewGuid(), "n", TestLeave.NowUtc);
        },
    };

    [Theory]
    [MemberData(nameof(ApprovableSourceStates))]
    public void Approve_Succeeds_FromDraftSubmittedOrUnderReview(Action<LeaveAdjustment> moveToSourceState)
    {
        ArgumentNullException.ThrowIfNull(moveToSourceState);

        var adjustment = NewAdjustment();
        moveToSourceState(adjustment);

        var result = adjustment.Approve(Guid.NewGuid(), ApprovedDecision(Guid.NewGuid()), TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        adjustment.Status.Should().Be(LeaveAdjustmentStatus.Approved);
    }

    [Fact]
    public void Approve_RaisesApprovedEvent_CarryingTheLeaveBalanceIdAndRequestedAmount()
    {
        var adjustment = NewAdjustment();
        var approverId = Guid.NewGuid();

        adjustment.Approve(approverId, ApprovedDecision(approverId), TestLeave.NowUtc);

        var raised = adjustment.DomainEvents.OfType<LeaveAdjustmentApproved>().Single();
        raised.LeaveBalanceId.Should().Be(_leaveBalanceId);
        raised.RequestedAmount.Should().Be(2m);
        raised.ApproverId.Should().Be(approverId);
    }

    [Fact]
    public void Approve_IsIdempotent_WhenAlreadyApproved()
    {
        var adjustment = NewAdjustment();
        var decision = ApprovedDecision(Guid.NewGuid());
        adjustment.Approve(Guid.NewGuid(), decision, TestLeave.NowUtc);
        adjustment.ClearDomainEvents();

        var result = adjustment.Approve(Guid.NewGuid(), decision, TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        adjustment.DomainEvents.Should().BeEmpty("an idempotent no-op must not re-raise the event a real transition already raised once");
    }

    [Fact]
    public void Approve_IsIdempotent_WhenAlreadyApplied()
    {
        var adjustment = NewAdjustment();
        adjustment.Approve(Guid.NewGuid(), ApprovedDecision(Guid.NewGuid()), TestLeave.NowUtc);
        adjustment.MarkApplied(TestLeave.NowUtc);
        adjustment.ClearDomainEvents();

        var result = adjustment.Approve(Guid.NewGuid(), ApprovedDecision(Guid.NewGuid()), TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        adjustment.Status.Should().Be(LeaveAdjustmentStatus.Applied, "approving an already-applied adjustment must not regress its status");
        adjustment.DomainEvents.Should().BeEmpty();
    }

    [Theory]
    [InlineData(LeaveAdjustmentStatus.Rejected)]
    [InlineData(LeaveAdjustmentStatus.Cancelled)]
    public void Approve_Fails_WhenRejectedOrCancelled(LeaveAdjustmentStatus terminalStatus)
    {
        var adjustment = NewAdjustment();
        MoveTo(adjustment, terminalStatus);

        var result = adjustment.Approve(Guid.NewGuid(), ApprovedDecision(Guid.NewGuid()), TestLeave.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.AdjustmentNotInReviewableState);
    }

    // ---- Reject (leave-adjustments.md: a branch from Under Review only) ----

    [Fact]
    public void Reject_Succeeds_FromUnderReview()
    {
        var adjustment = NewAdjustment();
        MoveTo(adjustment, LeaveAdjustmentStatus.UnderReview);

        var result = adjustment.Reject(Guid.NewGuid(), "not justified", TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        adjustment.Status.Should().Be(LeaveAdjustmentStatus.Rejected);
        adjustment.RejectionReason.Should().Be("not justified");
    }

    [Fact]
    public void Reject_Fails_WhenAlreadyApproved()
    {
        var adjustment = NewAdjustment();
        MoveTo(adjustment, LeaveAdjustmentStatus.Approved);

        var result = adjustment.Reject(Guid.NewGuid(), "reason", TestLeave.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.AdjustmentNotInReviewableState);
    }

    // ---- Cancel ------------------------------------------------------

    [Theory]
    [InlineData(LeaveAdjustmentStatus.Draft)]
    [InlineData(LeaveAdjustmentStatus.Submitted)]
    [InlineData(LeaveAdjustmentStatus.UnderReview)]
    [InlineData(LeaveAdjustmentStatus.Approved)]
    public void Cancel_Succeeds_FromEveryNonTerminalState(LeaveAdjustmentStatus sourceStatus)
    {
        var adjustment = NewAdjustment();
        MoveTo(adjustment, sourceStatus);

        var result = adjustment.Cancel(TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        adjustment.Status.Should().Be(LeaveAdjustmentStatus.Cancelled);
    }

    [Fact]
    public void Cancel_Fails_WhenApplied()
    {
        var adjustment = NewAdjustment();
        MoveTo(adjustment, LeaveAdjustmentStatus.Applied);

        var result = adjustment.Cancel(TestLeave.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.AdjustmentNotInReviewableState);
    }

    [Fact]
    public void Cancel_Fails_WhenAlreadyCancelled()
    {
        var adjustment = NewAdjustment();
        adjustment.Cancel(TestLeave.NowUtc);

        var result = adjustment.Cancel(TestLeave.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.AdjustmentNotInReviewableState);
    }

    // ---- MarkApplied ---------------------------------------------------

    [Fact]
    public void MarkApplied_Succeeds_WhenApproved()
    {
        var adjustment = NewAdjustment();
        MoveTo(adjustment, LeaveAdjustmentStatus.Approved);

        var result = adjustment.MarkApplied(TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        adjustment.Status.Should().Be(LeaveAdjustmentStatus.Applied);
        adjustment.AppliedAt.Should().Be(TestLeave.NowUtc);
    }

    [Fact]
    public void MarkApplied_IsIdempotent_WhenAlreadyApplied()
    {
        var adjustment = NewAdjustment();
        MoveTo(adjustment, LeaveAdjustmentStatus.Applied);

        var result = adjustment.MarkApplied(TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        adjustment.Status.Should().Be(LeaveAdjustmentStatus.Applied);
    }

    [Theory]
    [InlineData(LeaveAdjustmentStatus.Draft)]
    [InlineData(LeaveAdjustmentStatus.Submitted)]
    [InlineData(LeaveAdjustmentStatus.UnderReview)]
    [InlineData(LeaveAdjustmentStatus.Cancelled)]
    public void MarkApplied_Fails_WhenNotApproved(LeaveAdjustmentStatus sourceStatus)
    {
        var adjustment = NewAdjustment();
        MoveTo(adjustment, sourceStatus);

        var result = adjustment.MarkApplied(TestLeave.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.AdjustmentNotApproved);
    }

    /// <summary>Drives a freshly created (Draft) adjustment to the given status via its own public transitions.</summary>
    private static void MoveTo(LeaveAdjustment adjustment, LeaveAdjustmentStatus status)
    {
        if (status == LeaveAdjustmentStatus.Draft)
        {
            return;
        }

        adjustment.Submit(TestLeave.NowUtc);
        if (status == LeaveAdjustmentStatus.Submitted)
        {
            return;
        }

        adjustment.Review(Guid.NewGuid(), "n", TestLeave.NowUtc);
        if (status == LeaveAdjustmentStatus.UnderReview)
        {
            return;
        }

        if (status == LeaveAdjustmentStatus.Rejected)
        {
            adjustment.Reject(Guid.NewGuid(), "n", TestLeave.NowUtc);
            return;
        }

        if (status == LeaveAdjustmentStatus.Cancelled)
        {
            adjustment.Cancel(TestLeave.NowUtc);
            return;
        }

        adjustment.Approve(Guid.NewGuid(), new ApprovalDecision(Guid.NewGuid(), ApprovalDecisionOutcome.Approved, TestLeave.NowUtc, null, null, null), TestLeave.NowUtc);
        if (status == LeaveAdjustmentStatus.Approved)
        {
            return;
        }

        adjustment.MarkApplied(TestLeave.NowUtc);
    }
}
