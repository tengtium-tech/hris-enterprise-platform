using FluentAssertions;
using FluentValidation;
using Hris.Modules.Attendance.Application.Commands;
using Hris.Modules.Attendance.Application.Dtos;
using Hris.Modules.Attendance.Application.Queries;
using Hris.Modules.Attendance.Domain;
using Hris.Testing.TenantIsolation;
using MediatR;
using Xunit;

namespace Hris.Modules.Attendance.Tests.Application;

/// <summary>
/// AttendanceRecord's command and query handlers, dispatched through a real MediatR
/// pipeline (validation -> handler -> transaction/SaveChanges) against a real
/// PostgreSQL instance via the shared tenant-isolation harness (HEP-111), per
/// docs/09-testing/unit-and-integration-testing.md §3.1 — "an in-memory provider is
/// not acceptable." Attendance is the first module built after that harness existed,
/// so its Application layer is tested this way from the start rather than against
/// NSubstitute doubles, which is what every earlier module's own test project does.
///
/// Every test dispatches through <see cref="ISender"/> and, where the assertion is
/// about persisted state, reads it back through the module's own queries rather than
/// reaching into <c>DbContext</c> directly — this exercises the same round trip a real
/// caller does, not an implementation detail of how the repository happens to store it.
/// </summary>
public sealed class AttendanceRecordApplicationTests : TenantIsolationTestBase
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _employeeId = Guid.NewGuid();
    private const string _scopeTarget = "company-1";

    public AttendanceRecordApplicationTests(TenantIsolationFixture fixture)
        : base(fixture)
    {
    }

    private ISender Sender => GetService<ISender>();

    private async Task SeedActivePolicyAsync()
    {
        var policy = AttendancePolicy.Create(
            new AttendancePolicyId(Guid.NewGuid()), _tenantId, "Standard", TestAttendance.DefaultPolicy(),
            TestAttendance.Today, Guid.NewGuid(), TestAttendance.NowUtc).Value;
        policy.Publish(TestAttendance.Today, Guid.NewGuid(), TestAttendance.NowUtc);
        policy.Assign(
            new PolicyAssignmentId(Guid.NewGuid()), PolicyScopeLevel.Company, _scopeTarget, TestAttendance.Today,
            null, Guid.NewGuid(), TestAttendance.NowUtc);

        await SeedAsync(policy).ConfigureAwait(false);
    }

    private async Task<Guid> CaptureAndReturnRecordIdAsync(TimeEventType eventType, DateTimeOffset at) =>
        (await Sender.Send(new CaptureTimeEventCommand(
            _tenantId, _employeeId, TestAttendance.Today, eventType, at, AttendanceSource.MobileApplication,
            null, null, null, Guid.NewGuid())).ConfigureAwait(false)).Value;

    // ---- CaptureTimeEvent ------------------------------------------------

    [Fact]
    public async Task CaptureTimeEvent_CreatesANewRecord_RetrievableByQuery()
    {
        var recordId = await CaptureAndReturnRecordIdAsync(TimeEventType.ClockIn, TestAttendance.At(9));

        var queryResult = await Sender.Send(new GetAttendanceRecordQuery(_tenantId, recordId));

        queryResult.IsSuccess.Should().BeTrue();
        queryResult.Value.Status.Should().Be(nameof(AttendanceStatus.Created));
        queryResult.Value.TimeEvents.Should().ContainSingle(e => e.EventType == nameof(TimeEventType.ClockIn));
    }

    [Fact]
    public async Task CaptureTimeEvent_AppendsToTheSameRecord_ForTheSameEmployeeAndWorkDate()
    {
        var firstId = await CaptureAndReturnRecordIdAsync(TimeEventType.ClockIn, TestAttendance.At(9));
        var secondId = await CaptureAndReturnRecordIdAsync(TimeEventType.ClockOut, TestAttendance.At(17));

        secondId.Should().Be(firstId, "the same employee and work date must resolve to one record");
        var record = (await Sender.Send(new GetAttendanceRecordQuery(_tenantId, firstId))).Value;
        record.TimeEvents.Should().HaveCount(2);
    }

    [Fact]
    public async Task CaptureTimeEvent_Fails_ForADuplicateWithinTheDedupWindow()
    {
        var recordId = await CaptureAndReturnRecordIdAsync(TimeEventType.ClockIn, TestAttendance.At(9));

        var result = await Sender.Send(new CaptureTimeEventCommand(
            _tenantId, _employeeId, TestAttendance.Today, TimeEventType.ClockIn, TestAttendance.At(9).AddSeconds(20),
            AttendanceSource.MobileApplication, null, null, null, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.DuplicateTimeEvent);
        var record = (await Sender.Send(new GetAttendanceRecordQuery(_tenantId, recordId))).Value;
        record.TimeEvents.Should().ContainSingle("the duplicate must not reach the database");
    }

    [Fact]
    public async Task CaptureTimeEvent_Fails_WhenTheDeviceIsNotActive_AndPersistsNothing()
    {
        var device = AttendanceDevice.Create(
            new AttendanceDeviceId(Guid.NewGuid()), _tenantId, "Entrance", "SN-1", null, null, null,
            AttendanceDeviceType.Biometric, DeviceLocation.FromReference("loc"), default, Guid.NewGuid(), TestAttendance.NowUtc).Value;
        device.Retire(Guid.NewGuid(), "decommissioned", TestAttendance.NowUtc);
        await SeedAsync(device);

        var result = await Sender.Send(new CaptureTimeEventCommand(
            _tenantId, _employeeId, TestAttendance.Today, TimeEventType.ClockIn, TestAttendance.At(9),
            AttendanceSource.BiometricDevice, device.Id.Value, null, null, null));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.DeviceNotActive);
        var queryResult = await Sender.Send(new GetAttendanceForPeriodQuery(_tenantId, _employeeId, TestAttendance.Today, TestAttendance.Today));
        queryResult.Value.Should().BeEmpty("the handler checks the device before persisting even a brand-new record");
    }

    [Fact]
    public async Task CaptureTimeEvent_Fails_Validation_WhenEmployeeIdIsEmpty()
    {
        var act = () => Sender.Send(new CaptureTimeEventCommand(
            _tenantId, Guid.Empty, TestAttendance.Today, TimeEventType.ClockIn, TestAttendance.At(9),
            AttendanceSource.MobileApplication, null, null, null, null));

        await act.Should().ThrowAsync<ValidationException>("the ValidationBehavior rejects it before the handler runs");
    }

    // ---- RunCalculation ------------------------------------------------

    [Fact]
    public async Task RunCalculation_PersistsCalculatedFields_WhenAnEffectivePolicyMatches()
    {
        await SeedActivePolicyAsync();
        var recordId = await CaptureAndReturnRecordIdAsync(TimeEventType.ClockIn, TestAttendance.At(9));
        await Sender.Send(new CaptureTimeEventCommand(
            _tenantId, _employeeId, TestAttendance.Today, TimeEventType.ClockOut, TestAttendance.At(17),
            AttendanceSource.MobileApplication, null, null, null, null));

        var result = await Sender.Send(new RunCalculationCommand(
            _tenantId, recordId, "Manual", [_scopeTarget], null, Guid.NewGuid(), false, false, false, null, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        var record = (await Sender.Send(new GetAttendanceRecordQuery(_tenantId, recordId))).Value;
        record.Status.Should().Be(nameof(AttendanceStatus.Calculated));
        record.Calculated!.WorkingHours.Should().Be(8);
    }

    [Fact]
    public async Task RunCalculation_Fails_WhenNoEffectivePolicyMatchesTheScope()
    {
        var recordId = await CaptureAndReturnRecordIdAsync(TimeEventType.ClockIn, TestAttendance.At(9));

        var result = await Sender.Send(new RunCalculationCommand(
            _tenantId, recordId, "Manual", [_scopeTarget], null, null, false, false, false, null, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.AttendancePolicyNotFound);
    }

    [Fact]
    public async Task RunCalculation_IncludesApprovedOvertimeHours_InPayableHours()
    {
        await SeedActivePolicyAsync();
        var recordId = await CaptureAndReturnRecordIdAsync(TimeEventType.ClockIn, TestAttendance.At(9));
        await Sender.Send(new CaptureTimeEventCommand(
            _tenantId, _employeeId, TestAttendance.Today, TimeEventType.ClockOut, TestAttendance.At(18),
            AttendanceSource.MobileApplication, null, null, null, null));
        var overtime = OvertimeRequest.Create(
            new OvertimeRequestId(Guid.NewGuid()), _tenantId, _employeeId, TestAttendance.Today, null, null, 2,
            OvertimeCategory.Project, "closing books", Guid.NewGuid(), TestAttendance.NowUtc).Value;
        overtime.Approve(Guid.NewGuid(), TestAttendance.NowUtc);
        await SeedAsync(overtime);

        var result = await Sender.Send(new RunCalculationCommand(
            _tenantId, recordId, "Manual", [_scopeTarget], null, Guid.NewGuid(), false, false, false, null, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        var record = (await Sender.Send(new GetAttendanceRecordQuery(_tenantId, recordId))).Value;
        record.Calculated!.PayableHours.Should().Be(11, "9 worked hours plus 2 separately approved overtime hours");
    }

    // ---- Submit / Approve / Reject / Finalize / Reopen --------------------

    [Fact]
    public async Task FullApprovalLifecycle_TransitionsThroughEveryStatus()
    {
        await SeedActivePolicyAsync();
        var recordId = await CaptureAndReturnRecordIdAsync(TimeEventType.ClockIn, TestAttendance.At(9));
        await Sender.Send(new CaptureTimeEventCommand(
            _tenantId, _employeeId, TestAttendance.Today, TimeEventType.ClockOut, TestAttendance.At(17),
            AttendanceSource.MobileApplication, null, null, null, null));
        await Sender.Send(new RunCalculationCommand(
            _tenantId, recordId, "Manual", [_scopeTarget], null, Guid.NewGuid(), false, false, false, null, Guid.NewGuid()));

        (await Sender.Send(new SubmitAttendanceForApprovalCommand(_tenantId, recordId, Guid.NewGuid()))).IsSuccess.Should().BeTrue();
        (await StatusAsync(recordId)).Should().Be(nameof(AttendanceStatus.Submitted));

        (await Sender.Send(new ApproveAttendanceCommand(_tenantId, recordId, Guid.NewGuid(), ApprovalDecisionOutcome.Approved, null, null, null)))
            .IsSuccess.Should().BeTrue();
        (await StatusAsync(recordId)).Should().Be(nameof(AttendanceStatus.Approved));

        (await Sender.Send(new FinalizeAttendanceCommand(_tenantId, recordId, Guid.NewGuid()))).IsSuccess.Should().BeTrue();
        (await StatusAsync(recordId)).Should().Be(nameof(AttendanceStatus.Finalized));

        (await Sender.Send(new ReopenAttendanceCommand(_tenantId, recordId, Guid.NewGuid(), "AUTH-1", "correction")))
            .IsSuccess.Should().BeTrue();
        (await StatusAsync(recordId)).Should().Be(nameof(AttendanceStatus.Calculated));
    }

    [Fact]
    public async Task Reject_ReturnsASubmittedRecordToCalculated()
    {
        await SeedActivePolicyAsync();
        var recordId = await CaptureAndReturnRecordIdAsync(TimeEventType.ClockIn, TestAttendance.At(9));
        await Sender.Send(new CaptureTimeEventCommand(
            _tenantId, _employeeId, TestAttendance.Today, TimeEventType.ClockOut, TestAttendance.At(17),
            AttendanceSource.MobileApplication, null, null, null, null));
        await Sender.Send(new RunCalculationCommand(
            _tenantId, recordId, "Manual", [_scopeTarget], null, Guid.NewGuid(), false, false, false, null, Guid.NewGuid()));
        await Sender.Send(new SubmitAttendanceForApprovalCommand(_tenantId, recordId, Guid.NewGuid()));

        var result = await Sender.Send(new RejectAttendanceCommand(_tenantId, recordId, Guid.NewGuid(), "hours look wrong"));

        result.IsSuccess.Should().BeTrue();
        (await StatusAsync(recordId)).Should().Be(nameof(AttendanceStatus.Calculated));
    }

    [Fact]
    public async Task Finalize_Fails_WhileAnAdjustmentIsPending()
    {
        await SeedActivePolicyAsync();
        var recordId = await CaptureAndReturnRecordIdAsync(TimeEventType.ClockIn, TestAttendance.At(9));
        await Sender.Send(new CaptureTimeEventCommand(
            _tenantId, _employeeId, TestAttendance.Today, TimeEventType.ClockOut, TestAttendance.At(17),
            AttendanceSource.MobileApplication, null, null, null, null));
        await Sender.Send(new RunCalculationCommand(
            _tenantId, recordId, "Manual", [_scopeTarget], null, Guid.NewGuid(), false, false, false, null, Guid.NewGuid()));
        await Sender.Send(new SubmitAttendanceForApprovalCommand(_tenantId, recordId, Guid.NewGuid()));
        await Sender.Send(new ApproveAttendanceCommand(_tenantId, recordId, Guid.NewGuid(), ApprovalDecisionOutcome.Approved, null, null, null));
        await Sender.Send(new MarkRecordAdjustmentSubmittedCommand(_tenantId, recordId));

        var result = await Sender.Send(new FinalizeAttendanceCommand(_tenantId, recordId, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.PendingAdjustmentBlocksFinalization);
    }

    private async Task<string> StatusAsync(Guid recordId) =>
        (await Sender.Send(new GetAttendanceRecordQuery(_tenantId, recordId)).ConfigureAwait(false)).Value.Status;

    // ---- Queries ------------------------------------------------------

    [Fact]
    public async Task GetAttendanceRecordQuery_ReturnsNotFound_ForAnotherTenantsRecord()
    {
        var recordId = await CaptureAndReturnRecordIdAsync(TimeEventType.ClockIn, TestAttendance.At(9));

        var result = await Sender.Send(new GetAttendanceRecordQuery(Guid.NewGuid(), recordId));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.AttendanceRecordNotFound, "cross-tenant access must read as not-found, never a permission error");
    }

    [Fact]
    public async Task GetAttendanceForPeriodQuery_ReturnsOnlyRecordsWithinRange_OrderedByWorkDate()
    {
        await CaptureOnDateAsync(TestAttendance.Today.AddDays(-10));
        await CaptureOnDateAsync(TestAttendance.Today.AddDays(-2));
        await CaptureOnDateAsync(TestAttendance.Today);

        var result = await Sender.Send(new GetAttendanceForPeriodQuery(
            _tenantId, _employeeId, TestAttendance.Today.AddDays(-5), TestAttendance.Today));

        result.Value.Should().HaveCount(2);
        result.Value.Select(r => r.WorkDate).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetFinalizedAttendanceForPayrollPeriodQuery_ExcludesARecordWithAPendingAdjustment()
    {
        await SeedActivePolicyAsync();
        var recordId = await CaptureAndReturnRecordIdAsync(TimeEventType.ClockIn, TestAttendance.At(9));
        await Sender.Send(new CaptureTimeEventCommand(
            _tenantId, _employeeId, TestAttendance.Today, TimeEventType.ClockOut, TestAttendance.At(17),
            AttendanceSource.MobileApplication, null, null, null, null));
        await Sender.Send(new RunCalculationCommand(
            _tenantId, recordId, "Manual", [_scopeTarget], null, Guid.NewGuid(), false, false, false, null, Guid.NewGuid()));
        await Sender.Send(new SubmitAttendanceForApprovalCommand(_tenantId, recordId, Guid.NewGuid()));
        await Sender.Send(new ApproveAttendanceCommand(_tenantId, recordId, Guid.NewGuid(), ApprovalDecisionOutcome.Approved, null, null, null));
        await Sender.Send(new FinalizeAttendanceCommand(_tenantId, recordId, Guid.NewGuid()));
        await Sender.Send(new MarkRecordAdjustmentSubmittedCommand(_tenantId, recordId));

        var result = await Sender.Send(new GetFinalizedAttendanceForPayrollPeriodQuery(
            _tenantId, TestAttendance.Today, TestAttendance.Today));

        result.Value.Should().BeEmpty("a finalized record with a pending adjustment is not yet payroll-stable");
    }

    private async Task CaptureOnDateAsync(DateOnly workDate) =>
        await Sender.Send(new CaptureTimeEventCommand(
            _tenantId, _employeeId, workDate, TimeEventType.ClockIn, new DateTimeOffset(workDate, new TimeOnly(9, 0), TimeSpan.Zero),
            AttendanceSource.MobileApplication, null, null, null, null)).ConfigureAwait(false);

    [Fact]
    public async Task GetTeamAttendanceForApprovalQuery_ReturnsOnlyRecordsWithPendingApproval_ForTheGivenTeam()
    {
        await SeedActivePolicyAsync();
        var recordId = await CaptureAndReturnRecordIdAsync(TimeEventType.ClockIn, TestAttendance.At(9));
        await Sender.Send(new CaptureTimeEventCommand(
            _tenantId, _employeeId, TestAttendance.Today, TimeEventType.ClockOut, TestAttendance.At(17),
            AttendanceSource.MobileApplication, null, null, null, null));
        await Sender.Send(new RunCalculationCommand(
            _tenantId, recordId, "Manual", [_scopeTarget], null, Guid.NewGuid(), false, false, false, null, Guid.NewGuid()));
        await Sender.Send(new SubmitAttendanceForApprovalCommand(_tenantId, recordId, Guid.NewGuid()));

        var result = await Sender.Send(new GetTeamAttendanceForApprovalQuery(_tenantId, [_employeeId]));

        result.Value.Should().ContainSingle(r => r.Id == recordId && r.ApprovalStatus == nameof(ApprovalStatus.Pending));
    }

    [Fact]
    public async Task GetTeamAttendanceForApprovalQuery_ExcludesEmployeesNotInTheGivenTeam()
    {
        await SeedActivePolicyAsync();
        var recordId = await CaptureAndReturnRecordIdAsync(TimeEventType.ClockIn, TestAttendance.At(9));
        await Sender.Send(new CaptureTimeEventCommand(
            _tenantId, _employeeId, TestAttendance.Today, TimeEventType.ClockOut, TestAttendance.At(17),
            AttendanceSource.MobileApplication, null, null, null, null));
        await Sender.Send(new RunCalculationCommand(
            _tenantId, recordId, "Manual", [_scopeTarget], null, Guid.NewGuid(), false, false, false, null, Guid.NewGuid()));
        await Sender.Send(new SubmitAttendanceForApprovalCommand(_tenantId, recordId, Guid.NewGuid()));

        var result = await Sender.Send(new GetTeamAttendanceForApprovalQuery(_tenantId, [Guid.NewGuid()]));

        result.Value.Should().BeEmpty("the only pending record belongs to an employee outside the requested team");
    }

    [Fact]
    public async Task GetAttendanceExceptionsQuery_ReturnsARecordFlaggedDuringCalculation()
    {
        await SeedActivePolicyAsync();
        var recordId = await CaptureAndReturnRecordIdAsync(TimeEventType.ClockIn, TestAttendance.At(9));
        await Sender.Send(new CaptureTimeEventCommand(
            _tenantId, _employeeId, TestAttendance.Today, TimeEventType.ClockOut, TestAttendance.At(17),
            AttendanceSource.MobileApplication, null, null, null, null));

        await Sender.Send(new RunCalculationCommand(
            _tenantId, recordId, "Manual", [_scopeTarget], null, null, true, false, false, null, Guid.NewGuid()));

        var result = await Sender.Send(new GetAttendanceExceptionsQuery(_tenantId));

        result.Value.Should().ContainSingle(r => r.Id == recordId);
        result.Value.Single().Exceptions.Should().Contain(e => e.Contains("Shift could not be resolved", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetAttendanceExceptionsQuery_ScopesToTheGivenEmployeeIds_WhenProvided()
    {
        await SeedActivePolicyAsync();
        var recordId = await CaptureAndReturnRecordIdAsync(TimeEventType.ClockIn, TestAttendance.At(9));
        await Sender.Send(new CaptureTimeEventCommand(
            _tenantId, _employeeId, TestAttendance.Today, TimeEventType.ClockOut, TestAttendance.At(17),
            AttendanceSource.MobileApplication, null, null, null, null));
        await Sender.Send(new RunCalculationCommand(
            _tenantId, recordId, "Manual", [_scopeTarget], null, null, true, false, false, null, Guid.NewGuid()));

        var result = await Sender.Send(new GetAttendanceExceptionsQuery(_tenantId, [Guid.NewGuid()]));

        result.Value.Should().BeEmpty("the only exception record belongs to an employee outside the requested scope");
    }
}
