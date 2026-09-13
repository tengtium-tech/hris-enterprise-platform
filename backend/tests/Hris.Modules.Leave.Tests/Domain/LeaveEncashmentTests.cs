using FluentAssertions;
using Hris.Modules.Leave.Domain;
using Hris.SharedKernel;
using Xunit;

namespace Hris.Modules.Leave.Tests.Domain;

/// <summary>
/// LV-070 through LV-074: a simpler, four-reachable-state shape than
/// <see cref="LeaveAdjustment"/>'s own seven, since encashment is created directly at
/// PendingApproval (mirroring <see cref="LeaveRequest"/>'s own creation shape) rather than
/// starting in Draft. <see cref="LeaveEncashment.Cancel"/> is deliberately stricter than
/// <see cref="LeaveRequest.Cancel"/> — pending-only, matching leave-encashments.md's own
/// lifecycle diagram, which draws Cancelled as a branch from Pending Approval only.
/// </summary>
public sealed class LeaveEncashmentTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _employeeId = Guid.NewGuid();
    private readonly LeaveBalanceId _leaveBalanceId = new(Guid.NewGuid());

    private Result<LeaveEncashment> CreateResult(
        decimal requestedAmount = 5m, bool isCommutable = true, decimal? maximumCommutable = 10m, decimal availableBalance = 20m) =>
        LeaveEncashment.Create(
            new LeaveEncashmentId(Guid.NewGuid()), _tenantId, _employeeId, _leaveBalanceId, requestedAmount, isCommutable,
            maximumCommutable, availableBalance, Guid.NewGuid(), TestLeave.NowUtc);

    private LeaveEncashment NewEncashment() => CreateResult().Value;

    private static ApprovalDecision ApprovedDecision(Guid approverId) =>
        new(approverId, ApprovalDecisionOutcome.Approved, TestLeave.NowUtc, null, null, null);

    // ---- Create --------------------------------------------------------

    [Fact]
    public void Create_StartsPendingApproval_AndRaisesRequestedEvent()
    {
        var encashment = NewEncashment();

        encashment.Status.Should().Be(LeaveEncashmentStatus.PendingApproval);
        encashment.RequestedAmount.Should().Be(5m);
        encashment.DomainEvents.OfType<LeaveEncashmentRequested>().Should().ContainSingle();
    }

    [Fact]
    public void Create_Fails_WhenEmployeeIdentifierIsEmpty()
    {
        var result = LeaveEncashment.Create(
            new LeaveEncashmentId(Guid.NewGuid()), _tenantId, Guid.Empty, _leaveBalanceId, 5m, true, 10m, 20m,
            Guid.NewGuid(), TestLeave.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.EmployeeIdentifierRequired);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_Fails_WhenRequestedAmountIsNotPositive(decimal amount)
    {
        var result = CreateResult(requestedAmount: amount);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.LedgerEntryAmountMustBePositive);
    }

    [Fact]
    public void Create_Fails_WhenNotCommutable()
    {
        var result = CreateResult(isCommutable: false);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.EncashmentNotCommutable);
    }

    [Fact]
    public void Create_Fails_WhenRequestedAmountExceedsMaximumCommutable()
    {
        var result = CreateResult(requestedAmount: 15m, maximumCommutable: 10m);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.EncashmentExceedsCommutableCap);
    }

    [Fact]
    public void Create_Succeeds_WhenMaximumCommutableIsUnset()
    {
        var result = CreateResult(requestedAmount: 15m, maximumCommutable: null, availableBalance: 20m);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_Fails_WhenRequestedAmountExceedsAvailableBalance()
    {
        var result = CreateResult(requestedAmount: 5m, maximumCommutable: null, availableBalance: 4m);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.InsufficientBalance);
    }

    // ---- Approve -----------------------------------------------------

    [Fact]
    public void Approve_TransitionsToApproved_FromPendingApproval()
    {
        var encashment = NewEncashment();
        var approverId = Guid.NewGuid();

        var result = encashment.Approve(approverId, ApprovedDecision(approverId), TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        encashment.Status.Should().Be(LeaveEncashmentStatus.Approved);
        encashment.Decision!.ApproverId.Should().Be(approverId);
        encashment.DomainEvents.OfType<LeaveEncashmentApproved>().Should().ContainSingle();
    }

    [Fact]
    public void Approve_Fails_WhenNotPendingApproval()
    {
        var encashment = NewEncashment();
        var approverId = Guid.NewGuid();
        encashment.Approve(approverId, ApprovedDecision(approverId), TestLeave.NowUtc);

        var result = encashment.Approve(approverId, ApprovedDecision(approverId), TestLeave.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.EncashmentNotPendingApproval);
    }

    // ---- Reject --------------------------------------------------------

    [Fact]
    public void Reject_TransitionsToRejected_FromPendingApproval()
    {
        var encashment = NewEncashment();
        var approverId = Guid.NewGuid();

        var result = encashment.Reject(approverId, "policy cap exceeded", TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        encashment.Status.Should().Be(LeaveEncashmentStatus.Rejected);
        encashment.RejectionReason.Should().Be("policy cap exceeded");
        encashment.DomainEvents.OfType<LeaveEncashmentRejected>().Should().ContainSingle();
    }

    [Fact]
    public void Reject_Fails_WhenAlreadyApproved()
    {
        var encashment = NewEncashment();
        var approverId = Guid.NewGuid();
        encashment.Approve(approverId, ApprovedDecision(approverId), TestLeave.NowUtc);

        var result = encashment.Reject(approverId, "too late", TestLeave.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.EncashmentNotPendingApproval);
    }

    // ---- Cancel --------------------------------------------------------

    [Fact]
    public void Cancel_TransitionsToCancelled_FromPendingApproval()
    {
        var encashment = NewEncashment();
        var actorId = Guid.NewGuid();

        var result = encashment.Cancel(actorId, "no longer needed", TestLeave.NowUtc);

        result.IsSuccess.Should().BeTrue();
        encashment.Status.Should().Be(LeaveEncashmentStatus.Cancelled);
        encashment.CancellationReason.Should().Be("no longer needed");
        encashment.DomainEvents.OfType<LeaveEncashmentCancelled>().Should().ContainSingle();
    }

    [Fact]
    public void Cancel_Fails_WhenAlreadyApproved()
    {
        var encashment = NewEncashment();
        var approverId = Guid.NewGuid();
        encashment.Approve(approverId, ApprovedDecision(approverId), TestLeave.NowUtc);

        var result = encashment.Cancel(approverId, "changed my mind", TestLeave.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LeaveErrors.EncashmentNotCancellable);
    }
}
