using FluentAssertions;
using FluentValidation;
using Hris.Modules.Attendance.Application.Commands;
using Hris.Modules.Attendance.Application.Queries;
using Hris.Modules.Attendance.Domain;
using Hris.Testing.TenantIsolation;
using MediatR;
using Xunit;

namespace Hris.Modules.Attendance.Tests.Application;

/// <summary>
/// AttendanceAdjustment's command and query handlers, dispatched through the real MediatR
/// pipeline against the shared tenant-isolation harness (HEP-111), following the same
/// pattern as <see cref="AttendanceRecordApplicationTests"/>. AT-024's two-phase
/// approve/apply split means <see cref="ApproveAttendanceAdjustmentCommand"/> only ever
/// touches this aggregate; <see cref="ApplyAttendanceAdjustmentCommand"/> and
/// <see cref="MarkAdjustmentAppliedCommand"/> are the separate, system-issued commands
/// that carry the outcome to the record in its own transaction, and are exercised here too.
///
/// The domain's own <c>Submit()</c> transition (Draft -> Submitted) has no wrapping
/// application command in this sprint -- <see cref="SubmitAttendanceAdjustmentCommand"/>
/// actually maps to <c>AttendanceAdjustment.Create</c>, which already starts in Draft.
/// Tests that need a Submitted or UnderReview adjustment therefore seed one built directly
/// through the aggregate's own public transitions, the same way
/// <see cref="AttendanceRecordApplicationTests"/> seeds an <c>OvertimeRequest</c> it does
/// not itself construct through a command.
/// </summary>
public sealed class AttendanceAdjustmentApplicationTests : TenantIsolationTestBase
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _employeeId = Guid.NewGuid();

    public AttendanceAdjustmentApplicationTests(TenantIsolationFixture fixture)
        : base(fixture)
    {
    }

    private ISender Sender => GetService<ISender>();

    private async Task<Guid> CreateRecordAsync() =>
        (await Sender.Send(new CaptureTimeEventCommand(
            _tenantId, _employeeId, TestAttendance.Today, TimeEventType.ClockIn, TestAttendance.At(9),
            AttendanceSource.MobileApplication, null, null, null, Guid.NewGuid())).ConfigureAwait(false)).Value;

    private async Task<Guid> SeedAdjustmentAsync(Action<AttendanceAdjustment>? moveToState = null)
    {
        var adjustment = TestAttendance.Adjustment(tenantId: _tenantId);
        moveToState?.Invoke(adjustment);
        await SeedAsync(adjustment).ConfigureAwait(false);
        return adjustment.Id.Value;
    }

    private static ApprovalDecision ApprovedDecision(Guid approverId) =>
        new(approverId, ApprovalDecisionOutcome.Approved, TestAttendance.NowUtc, null, null, null);

    // ---- Submit (Create) ------------------------------------------------

    [Fact]
    public async Task Submit_CreatesAnAdjustmentInDraft_RetrievableByQuery()
    {
        var recordId = await CreateRecordAsync();

        var result = await Sender.Send(new SubmitAttendanceAdjustmentCommand(
            _tenantId, recordId, "ClockOut", "18:00", "18:30", AdjustmentCategory.ForgotClockOut,
            "Forgot to clock out", null, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        var queryResult = await Sender.Send(new GetAttendanceAdjustmentQuery(_tenantId, result.Value));
        queryResult.Value.Status.Should().Be(nameof(AdjustmentStatus.Draft));
        queryResult.Value.OriginalValue.Should().Be("18:00");
        queryResult.Value.RequestedValue.Should().Be("18:30");
    }

    [Fact]
    public async Task Submit_Fails_WhenTheAttendanceRecordDoesNotExist()
    {
        var result = await Sender.Send(new SubmitAttendanceAdjustmentCommand(
            _tenantId, Guid.NewGuid(), "ClockOut", "18:00", "18:30", AdjustmentCategory.ForgotClockOut,
            "reason", null, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.AttendanceRecordNotFound);
    }

    [Fact]
    public async Task Submit_Fails_Validation_WhenReasonIsEmpty()
    {
        var recordId = await CreateRecordAsync();

        var act = () => Sender.Send(new SubmitAttendanceAdjustmentCommand(
            _tenantId, recordId, "ClockOut", "18:00", "18:30", AdjustmentCategory.ForgotClockOut,
            string.Empty, null, Guid.NewGuid()));

        await act.Should().ThrowAsync<ValidationException>("the ValidationBehavior rejects it before the handler runs");
    }

    // ---- Review ------------------------------------------------------

    [Fact]
    public async Task Review_TransitionsToUnderReview_FromSubmitted()
    {
        var adjustmentId = await SeedAdjustmentAsync(a => a.Submit(Guid.NewGuid(), TestAttendance.NowUtc));
        var reviewerId = Guid.NewGuid();

        var result = await Sender.Send(new ReviewAttendanceAdjustmentCommand(_tenantId, adjustmentId, reviewerId, "looks reasonable"));

        result.IsSuccess.Should().BeTrue();
        var adjustment = (await Sender.Send(new GetAttendanceAdjustmentQuery(_tenantId, adjustmentId))).Value;
        adjustment.Status.Should().Be(nameof(AdjustmentStatus.UnderReview));
        adjustment.ReviewerId.Should().Be(reviewerId);
    }

    [Fact]
    public async Task Review_Fails_WhileStillDraft()
    {
        var adjustmentId = await SeedAdjustmentAsync();

        var result = await Sender.Send(new ReviewAttendanceAdjustmentCommand(_tenantId, adjustmentId, Guid.NewGuid(), "notes"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.AdjustmentNotInReviewableState);
    }

    // ---- Approve -------------------------------------------------------

    [Fact]
    public async Task Approve_Succeeds_FromDraft_AndPersistsTheDecision()
    {
        var adjustmentId = await SeedAdjustmentAsync();
        var approverId = Guid.NewGuid();

        var result = await Sender.Send(new ApproveAttendanceAdjustmentCommand(
            _tenantId, adjustmentId, approverId, ApprovalDecisionOutcome.Approved, "ok", null, null));

        result.IsSuccess.Should().BeTrue();
        var adjustment = (await Sender.Send(new GetAttendanceAdjustmentQuery(_tenantId, adjustmentId))).Value;
        adjustment.Status.Should().Be(nameof(AdjustmentStatus.Approved));
        adjustment.Decision!.ApproverId.Should().Be(approverId);
    }

    [Fact]
    public async Task Approve_Fails_WhenAlreadyRejected()
    {
        var adjustmentId = await SeedAdjustmentAsync(a => a.Reject(Guid.NewGuid(), "n", TestAttendance.NowUtc));

        var result = await Sender.Send(new ApproveAttendanceAdjustmentCommand(
            _tenantId, adjustmentId, Guid.NewGuid(), ApprovalDecisionOutcome.Approved, null, null, null));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.AdjustmentNotInReviewableState);
    }

    // ---- Reject --------------------------------------------------------

    [Fact]
    public async Task Reject_Succeeds_FromApproved()
    {
        var adjustmentId = await SeedAdjustmentAsync(a => a.Approve(Guid.NewGuid(), ApprovedDecision(Guid.NewGuid()), TestAttendance.NowUtc));

        var result = await Sender.Send(new RejectAttendanceAdjustmentCommand(_tenantId, adjustmentId, Guid.NewGuid(), "hours look wrong"));

        result.IsSuccess.Should().BeTrue();
        var adjustment = (await Sender.Send(new GetAttendanceAdjustmentQuery(_tenantId, adjustmentId))).Value;
        adjustment.Status.Should().Be(nameof(AdjustmentStatus.Rejected));
    }

    // ---- Cancel --------------------------------------------------------

    [Fact]
    public async Task Cancel_Succeeds_FromDraft()
    {
        var adjustmentId = await SeedAdjustmentAsync();

        var result = await Sender.Send(new CancelAttendanceAdjustmentCommand(_tenantId, adjustmentId, Guid.NewGuid(), "withdrawn"));

        result.IsSuccess.Should().BeTrue();
        var adjustment = (await Sender.Send(new GetAttendanceAdjustmentQuery(_tenantId, adjustmentId))).Value;
        adjustment.Status.Should().Be(nameof(AdjustmentStatus.Cancelled));
    }

    [Fact]
    public async Task Cancel_Fails_WhenAlreadyApplied()
    {
        var adjustmentId = await SeedAdjustmentAsync(a =>
        {
            a.Approve(Guid.NewGuid(), ApprovedDecision(Guid.NewGuid()), TestAttendance.NowUtc);
            a.MarkApplied();
        });

        var result = await Sender.Send(new CancelAttendanceAdjustmentCommand(_tenantId, adjustmentId, Guid.NewGuid(), "reason"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.AdjustmentNotInReviewableState);
    }

    // ---- ApplyAttendanceAdjustment / MarkAdjustmentApplied (AT-024) -------

    [Fact]
    public async Task ApplyAttendanceAdjustment_Succeeds_InItsOwnTransaction_AgainstTheRecord()
    {
        var recordId = await CreateRecordAsync();
        await Sender.Send(new CaptureTimeEventCommand(
            _tenantId, _employeeId, TestAttendance.Today, TimeEventType.ClockOut, TestAttendance.At(17),
            AttendanceSource.MobileApplication, null, null, null, null));
        var adjustmentId = await SeedAdjustmentAsync(a => a.Approve(Guid.NewGuid(), ApprovedDecision(Guid.NewGuid()), TestAttendance.NowUtc));

        var result = await Sender.Send(new ApplyAttendanceAdjustmentCommand(_tenantId, recordId, adjustmentId, "ClockOut", "18:30"));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAdjustmentApplied_TransitionsToApplied_WhenApproved()
    {
        var adjustmentId = await SeedAdjustmentAsync(a => a.Approve(Guid.NewGuid(), ApprovedDecision(Guid.NewGuid()), TestAttendance.NowUtc));

        var result = await Sender.Send(new MarkAdjustmentAppliedCommand(_tenantId, adjustmentId));

        result.IsSuccess.Should().BeTrue();
        var adjustment = (await Sender.Send(new GetAttendanceAdjustmentQuery(_tenantId, adjustmentId))).Value;
        adjustment.Status.Should().Be(nameof(AdjustmentStatus.Applied));
    }

    [Fact]
    public async Task MarkAdjustmentApplied_Fails_WhenNotApproved()
    {
        var adjustmentId = await SeedAdjustmentAsync();

        var result = await Sender.Send(new MarkAdjustmentAppliedCommand(_tenantId, adjustmentId));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.AdjustmentNotApproved);
    }

    // ---- GetPendingAdjustmentsForReview -----------------------------------

    [Fact]
    public async Task GetPendingAdjustmentsForReview_ReturnsSubmittedAndUnderReview_ButNotDraftOrTerminal()
    {
        await SeedAdjustmentAsync(a => a.Submit(Guid.NewGuid(), TestAttendance.NowUtc));
        await SeedAdjustmentAsync(a =>
        {
            a.Submit(Guid.NewGuid(), TestAttendance.NowUtc);
            a.Review(Guid.NewGuid(), "n", TestAttendance.NowUtc);
        });
        await SeedAdjustmentAsync();
        await SeedAdjustmentAsync(a => a.Cancel(Guid.NewGuid(), "n", TestAttendance.NowUtc));

        var result = await Sender.Send(new GetPendingAdjustmentsForReviewQuery(_tenantId));

        result.Value.Should().HaveCount(2);
        result.Value.Select(a => a.Status).Should().OnlyContain(
            s => s == nameof(AdjustmentStatus.Submitted) || s == nameof(AdjustmentStatus.UnderReview));
    }

    [Fact]
    public async Task GetAttendanceAdjustmentQuery_ReturnsNotFound_ForAnotherTenantsAdjustment()
    {
        var adjustmentId = await SeedAdjustmentAsync();

        var result = await Sender.Send(new GetAttendanceAdjustmentQuery(Guid.NewGuid(), adjustmentId));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.AttendanceAdjustmentNotFound);
    }
}
