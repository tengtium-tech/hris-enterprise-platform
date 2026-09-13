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
/// AttendancePolicy's command and query handlers, dispatched through the real MediatR
/// pipeline against the shared tenant-isolation harness (HEP-111). The read surface
/// (queries.md) exposes only <see cref="GetEffectiveAttendancePolicyQuery"/> -- there is
/// no "get one policy by id" query -- so tests that need to inspect a Draft or superseded
/// version (neither of which that query can ever return, since it only resolves Active
/// versions with a matching assignment) read back through
/// <see cref="IAttendancePolicyRepository"/> directly, the same repository the command and
/// query handlers themselves use.
/// </summary>
public sealed class AttendancePolicyApplicationTests : TenantIsolationTestBase
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private const string _scopeTarget = "company-1";

    public AttendancePolicyApplicationTests(TenantIsolationFixture fixture)
        : base(fixture)
    {
    }

    private ISender Sender => GetService<ISender>();

    private IAttendancePolicyRepository Repository => GetService<IAttendancePolicyRepository>();

    private async Task<Guid> DefineAndReturnIdAsync() =>
        (await Sender.Send(new DefineAttendancePolicyCommand(
            _tenantId, "Standard", TestAttendance.DefaultPolicy(), TestAttendance.Today, Guid.NewGuid())).ConfigureAwait(false)).Value;

    // ---- Define --------------------------------------------------------

    [Fact]
    public async Task Define_CreatesADraftPolicyAtVersionOne()
    {
        var policyId = await DefineAndReturnIdAsync();

        var policy = await Repository.GetByIdAsync(new AttendancePolicyId(policyId), CancellationToken.None);

        policy.Should().NotBeNull();
        policy!.Status.Should().Be(AttendancePolicyStatus.Draft);
        policy.Version.Should().Be(1);
    }

    [Fact]
    public async Task Define_Fails_Validation_WhenNameIsEmpty()
    {
        var act = () => Sender.Send(new DefineAttendancePolicyCommand(
            _tenantId, string.Empty, TestAttendance.DefaultPolicy(), TestAttendance.Today, Guid.NewGuid()));

        await act.Should().ThrowAsync<ValidationException>();
    }

    // ---- Publish / Assign -> effective lookup -----------------------------

    [Fact]
    public async Task Publish_AndAssign_MakeThePolicyEffectiveForItsScope()
    {
        var policyId = await DefineAndReturnIdAsync();
        await Sender.Send(new PublishAttendancePolicyCommand(_tenantId, policyId, TestAttendance.Today, Guid.NewGuid()));
        var assignmentId = Guid.NewGuid();
        await Sender.Send(new AssignAttendancePolicyCommand(
            _tenantId, policyId, assignmentId, PolicyScopeLevel.Company, _scopeTarget, TestAttendance.Today, null, Guid.NewGuid()));

        var result = await Sender.Send(new GetEffectiveAttendancePolicyQuery(_tenantId, [_scopeTarget], TestAttendance.Today));

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(policyId);
        result.Value.Assignments.Should().ContainSingle(a => a.Id == assignmentId);
    }

    [Fact]
    public async Task Publish_Fails_WhenNotDraft()
    {
        var policyId = await DefineAndReturnIdAsync();
        await Sender.Send(new PublishAttendancePolicyCommand(_tenantId, policyId, TestAttendance.Today, Guid.NewGuid()));

        var result = await Sender.Send(new PublishAttendancePolicyCommand(_tenantId, policyId, TestAttendance.Today, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.PolicyVersionNotDraft);
    }

    // ---- Revise (AT-002) -----------------------------------------------

    [Fact]
    public async Task Revise_PersistsANewDraftVersionRow_AndEndDatesTheCurrentOne()
    {
        var policyId = await DefineAndReturnIdAsync();
        await Sender.Send(new PublishAttendancePolicyCommand(_tenantId, policyId, TestAttendance.Today, Guid.NewGuid()));
        var newEffectiveFrom = TestAttendance.Today.AddMonths(1);

        var result = await Sender.Send(new ReviseAttendancePolicyCommand(
            _tenantId, policyId, TestAttendance.DefaultPolicy(overtimeEligible: false), newEffectiveFrom, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        var lineage = await Repository.ListByLineageAsync(policyId, CancellationToken.None);
        lineage.Should().HaveCount(2);
        var current = lineage.Single(p => p.Id.Value == policyId);
        var next = lineage.Single(p => p.Id.Value == result.Value);
        current.EffectiveTo.Should().Be(newEffectiveFrom.AddDays(-1), "the superseded version is end-dated, never deleted");
        next.Version.Should().Be(2);
        next.Status.Should().Be(AttendancePolicyStatus.Draft);
    }

    [Fact]
    public async Task Revise_Fails_WhenTheCurrentVersionIsNotActive()
    {
        var policyId = await DefineAndReturnIdAsync(); // still Draft

        var result = await Sender.Send(new ReviseAttendancePolicyCommand(
            _tenantId, policyId, TestAttendance.DefaultPolicy(), TestAttendance.Today.AddMonths(1), Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.PolicyVersionNotDraft);
    }

    // ---- Assign / Unassign (AT-011) -------------------------------------

    [Fact]
    public async Task Assign_Fails_WhenItOverlapsAnExistingAssignmentToTheSameScope()
    {
        var policyId = await DefineAndReturnIdAsync();
        await Sender.Send(new AssignAttendancePolicyCommand(
            _tenantId, policyId, Guid.NewGuid(), PolicyScopeLevel.Department, "dept-1", TestAttendance.Today, null, Guid.NewGuid()));

        var result = await Sender.Send(new AssignAttendancePolicyCommand(
            _tenantId, policyId, Guid.NewGuid(), PolicyScopeLevel.Department, "dept-1", TestAttendance.Today.AddDays(10), null, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.PolicyAssignmentOverlap);
    }

    [Fact]
    public async Task Unassign_EndDatesTheAssignment_SoItNoLongerCoversDatesPastTheNewEnd()
    {
        var policyId = await DefineAndReturnIdAsync();
        await Sender.Send(new PublishAttendancePolicyCommand(_tenantId, policyId, TestAttendance.Today, Guid.NewGuid()));
        var assignmentId = Guid.NewGuid();
        await Sender.Send(new AssignAttendancePolicyCommand(
            _tenantId, policyId, assignmentId, PolicyScopeLevel.Department, "dept-1", TestAttendance.Today, null, Guid.NewGuid()));
        var endDate = TestAttendance.Today.AddDays(5);

        var result = await Sender.Send(new UnassignAttendancePolicyCommand(_tenantId, policyId, assignmentId, endDate, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        (await Sender.Send(new GetEffectiveAttendancePolicyQuery(_tenantId, ["dept-1"], endDate))).IsSuccess.Should().BeTrue(
            "the end date itself is still covered");
        (await Sender.Send(new GetEffectiveAttendancePolicyQuery(_tenantId, ["dept-1"], endDate.AddDays(1)))).IsFailure.Should().BeTrue(
            "the assignment no longer covers a date past its new end");
    }

    // ---- Retire --------------------------------------------------------

    [Fact]
    public async Task Retire_TransitionsToRetired()
    {
        var policyId = await DefineAndReturnIdAsync();

        var result = await Sender.Send(new RetireAttendancePolicyCommand(_tenantId, policyId, Guid.NewGuid(), "superseded"));

        result.IsSuccess.Should().BeTrue();
        var policy = await Repository.GetByIdAsync(new AttendancePolicyId(policyId), CancellationToken.None);
        policy!.Status.Should().Be(AttendancePolicyStatus.Retired);
    }

    // ---- GetEffectiveAttendancePolicy ------------------------------------

    [Fact]
    public async Task GetEffectiveAttendancePolicy_Fails_WhenNoAssignmentMatchesTheScope()
    {
        var policyId = await DefineAndReturnIdAsync();
        await Sender.Send(new PublishAttendancePolicyCommand(_tenantId, policyId, TestAttendance.Today, Guid.NewGuid()));
        await Sender.Send(new AssignAttendancePolicyCommand(
            _tenantId, policyId, Guid.NewGuid(), PolicyScopeLevel.Department, "dept-1", TestAttendance.Today, null, Guid.NewGuid()));

        var result = await Sender.Send(new GetEffectiveAttendancePolicyQuery(_tenantId, ["dept-2"], TestAttendance.Today));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AttendanceErrors.AttendancePolicyNotFound);
    }
}
