using FluentAssertions;
using Hris.Modules.Timekeeping.Domain;
using Hris.SharedKernel;
using Xunit;

namespace Hris.Modules.Timekeeping.Tests.Domain;

public sealed class ShiftAssignmentTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private static DateOnly Today => TestTimekeeping.Today;

    private Result<ShiftAssignment> Create(
        AssignmentTargetType targetType = AssignmentTargetType.Employee,
        string? targetId = "emp-1",
        OrganizationalAssignmentLevel level = OrganizationalAssignmentLevel.IndividualEmployee,
        DateOnly? to = null,
        bool isTemporary = false,
        bool overlaps = false,
        Guid? assignedBy = null,
        Guid? rotationCycle = null) =>
        ShiftAssignment.Create(
            new ShiftAssignmentId(Guid.NewGuid()), _tenantId, targetType, targetId, level,
            new WorkShiftId(Guid.NewGuid()), Today, to, isTemporary,
            rotationCycle is null ? null : new RotationCycleId(rotationCycle.Value), overlaps,
            assignedBy ?? Guid.NewGuid(), TestTimekeeping.NowUtc);

    /// <summary>TK-032. A temporary assignment that had to be manually ended would eventually become permanent.</summary>
    [Fact]
    public void Create_Fails_WhenATemporaryAssignmentHasNoEndDate()
    {
        Create(isTemporary: true).Error.Should().Be(TimekeepingErrors.TemporaryAssignmentRequiresEndDate);
    }

    [Fact]
    public void Create_Succeeds_ForATemporaryAssignmentWithAnEndDate()
    {
        Create(isTemporary: true, to: Today.AddDays(14)).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_Fails_WhenTheTargetTypeContradictsTheLevel()
    {
        Create(targetType: AssignmentTargetType.OrganizationalUnit).Error
            .Should().Be(TimekeepingErrors.IndividualAssignmentRequiresEmployeeTarget);

        Create(targetType: AssignmentTargetType.Employee, level: OrganizationalAssignmentLevel.Department).Error
            .Should().Be(TimekeepingErrors.IndividualAssignmentRequiresEmployeeTarget);
    }

    [Fact]
    public void Create_Fails_WhenTheTargetIsMissing()
    {
        Create(targetId: " ").Error.Should().Be(TimekeepingErrors.AssignmentTargetRequired);
    }

    [Fact]
    public void Create_Fails_WhenTheEndDatePrecedesTheStart()
    {
        Create(to: Today.AddDays(-1)).Error.Should().Be(SharedKernelErrors.DateRangeEndBeforeStart);
    }

    /// <summary>TK-031, from the caller-supplied signal.</summary>
    [Fact]
    public void Create_Fails_WhenAnIndividualAssignmentOverlapsAnExistingOne()
    {
        Create(overlaps: true).Error.Should().Be(TimekeepingErrors.OverlappingIndividualAssignment);
    }

    [Fact]
    public void Create_IgnoresTheOverlapSignal_ForANonIndividualLevel()
    {
        var result = Create(
            targetType: AssignmentTargetType.OrganizationalUnit, targetId: "dept-1",
            level: OrganizationalAssignmentLevel.Department, overlaps: true);

        result.IsSuccess.Should().BeTrue("TK-031 constrains individual-level assignments specifically");
    }

    [Fact]
    public void Create_StartsScheduled_AndRaisesTheCreatedEvent()
    {
        var assignment = Create().Value;

        assignment.Status.Should().Be(ShiftAssignmentStatus.Scheduled);
        assignment.DomainEvents.OfType<ShiftAssignmentCreated>().Should().ContainSingle();
    }

    /// <summary>TK-051: a rotation-generated assignment records no actor rather than a placeholder.</summary>
    [Fact]
    public void Create_CarriesNoActor_ForARotationGeneratedAssignment()
    {
        var result = ShiftAssignment.Create(
            new ShiftAssignmentId(Guid.NewGuid()), _tenantId, AssignmentTargetType.Employee, "emp-1",
            OrganizationalAssignmentLevel.IndividualEmployee, new WorkShiftId(Guid.NewGuid()), Today, null, false,
            new RotationCycleId(Guid.NewGuid()), false, null, TestTimekeeping.NowUtc);

        result.Value.AssignedBy.Should().BeNull();
        result.Value.DomainEvents.OfType<ShiftAssignmentCreated>().Single().AssignedBy.Should().BeNull();
    }

    [Fact]
    public void Expire_EndsTheAssignmentWithNoActor()
    {
        var assignment = Create(isTemporary: true, to: Today.AddDays(5)).Value;

        var result = assignment.Expire(TestTimekeeping.NowUtc);

        result.IsSuccess.Should().BeTrue();
        assignment.Status.Should().Be(ShiftAssignmentStatus.Expired);
        assignment.DomainEvents.OfType<ShiftAssignmentExpired>().Should().ContainSingle();
    }

    [Fact]
    public void Expire_Fails_WhenTheAssignmentHasNoEndDate()
    {
        var assignment = Create().Value;

        assignment.Expire(TestTimekeeping.NowUtc).Error
            .Should().Be(TimekeepingErrors.TemporaryAssignmentRequiresEndDate);
    }

    [Fact]
    public void Expire_IsIdempotent()
    {
        var assignment = Create(isTemporary: true, to: Today.AddDays(5)).Value;
        assignment.Expire(TestTimekeeping.NowUtc);

        assignment.Expire(TestTimekeeping.NowUtc).IsSuccess.Should().BeTrue();
        assignment.DomainEvents.OfType<ShiftAssignmentExpired>().Should().ContainSingle();
    }

    /// <summary>TK-035: cancelled is retained and end-dated, never removed.</summary>
    [Fact]
    public void Cancel_EndDatesAndRetainsTheAssignment()
    {
        var assignment = Create().Value;

        var result = assignment.Cancel("Restructure", Guid.NewGuid(), Today, TestTimekeeping.NowUtc);

        result.IsSuccess.Should().BeTrue();
        assignment.Status.Should().Be(ShiftAssignmentStatus.Cancelled);
        assignment.EffectiveTo.Should().Be(Today);
        assignment.DomainEvents.OfType<ShiftAssignmentCancelled>().Should().ContainSingle();
    }

    [Fact]
    public void Cancel_IsIdempotent()
    {
        var assignment = Create().Value;
        assignment.Cancel("Restructure", Guid.NewGuid(), Today, TestTimekeeping.NowUtc);

        assignment.Cancel("Again", Guid.NewGuid(), Today, TestTimekeeping.NowUtc).IsSuccess.Should().BeTrue();
        assignment.DomainEvents.OfType<ShiftAssignmentCancelled>().Should().ContainSingle();
    }

    [Fact]
    public void AppliesOn_ExcludesCancelledAndSwappedAssignments()
    {
        var cancelled = Create().Value;
        cancelled.Cancel("Reason", Guid.NewGuid(), Today, TestTimekeeping.NowUtc);

        cancelled.AppliesOn(Today).Should().BeFalse();
    }

    [Fact]
    public void Activate_MovesFromScheduledToActive()
    {
        var assignment = Create().Value;

        assignment.Activate().IsSuccess.Should().BeTrue();
        assignment.Status.Should().Be(ShiftAssignmentStatus.Active);
        assignment.Activate().Error.Should().Be(TimekeepingErrors.AssignmentNotActiveOrScheduled);
    }
}

/// <summary>
/// TK-033 and TK-034, the four-step swap flow. commands.md requires propose, consent,
/// override, and activate to stay distinct because "a swap only one party has agreed
/// to is not yet a swap".
/// </summary>
public sealed class ShiftSwapTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private static DateOnly Today => TestTimekeeping.Today;

    private ShiftAssignment Assignment(string employeeId) =>
        TestTimekeeping.Assignment(_tenantId, employeeId);

    [Fact]
    public void ProposeSwap_RecordsTheProposalWithoutChangingEitherAssignment()
    {
        var primary = Assignment("emp-1");
        var secondary = Assignment("emp-2");

        var result = primary.ProposeSwap(secondary.Id, "emp-1", "emp-2", Today, Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
        primary.Status.Should().Be(ShiftAssignmentStatus.Scheduled, "proposing changes nothing yet");
        primary.PendingSwap.Should().NotBeNull();
        primary.PendingSwap!.IsReadyToActivate.Should().BeFalse();
    }

    [Fact]
    public void ProposeSwap_Fails_WhenSwappingWithItself()
    {
        var primary = Assignment("emp-1");

        primary.ProposeSwap(primary.Id, "emp-1", "emp-1", Today, Guid.NewGuid()).Error
            .Should().Be(TimekeepingErrors.SwapRequiresTwoDistinctAssignments);
    }

    /// <summary>
    /// Consent is matched to the named employee, so one party agreeing twice can never
    /// satisfy TK-034 — the failure a consent counter would allow.
    /// </summary>
    [Fact]
    public void ConsentToSwap_RequiresBothNamedParties_NotTwoConsentsFromOne()
    {
        var primary = Assignment("emp-1");
        var secondary = Assignment("emp-2");
        primary.ProposeSwap(secondary.Id, "emp-1", "emp-2", Today, Guid.NewGuid());

        primary.ConsentToSwap("emp-1");
        primary.ConsentToSwap("emp-1");

        primary.PendingSwap!.IsReadyToActivate.Should().BeFalse("only one of the two named parties has agreed");

        primary.ConsentToSwap("emp-2");
        primary.PendingSwap.IsReadyToActivate.Should().BeTrue();
    }

    [Fact]
    public void ConsentToSwap_Fails_ForAnUnrelatedEmployee()
    {
        var primary = Assignment("emp-1");
        var secondary = Assignment("emp-2");
        primary.ProposeSwap(secondary.Id, "emp-1", "emp-2", Today, Guid.NewGuid());

        primary.ConsentToSwap("emp-9").Error.Should().Be(TimekeepingErrors.SwapRequiresBothPartiesConsent);
    }

    [Fact]
    public void ConsentToSwap_Fails_WhenNoSwapWasProposed()
    {
        var primary = Assignment("emp-1");

        primary.ConsentToSwap("emp-1").Error.Should().Be(TimekeepingErrors.SwapRequiresBothPartiesConsent);
    }

    [Fact]
    public void OverrideSwapConsent_Fails_WithoutOverrideAuthority()
    {
        var primary = Assignment("emp-1");
        var secondary = Assignment("emp-2");
        primary.ProposeSwap(secondary.Id, "emp-1", "emp-2", Today, Guid.NewGuid());

        primary.OverrideSwapConsent(Guid.NewGuid(), "No-show cover", false).Error
            .Should().Be(TimekeepingErrors.SwapRequiresBothPartiesConsent);
    }

    [Fact]
    public void OverrideSwapConsent_SubstitutesForConsent_AndIsRecordedDistinctly()
    {
        var primary = Assignment("emp-1");
        var secondary = Assignment("emp-2");
        primary.ProposeSwap(secondary.Id, "emp-1", "emp-2", Today, Guid.NewGuid());
        var supervisor = Guid.NewGuid();

        var result = primary.OverrideSwapConsent(supervisor, "No-show cover", true);

        result.IsSuccess.Should().BeTrue();
        primary.PendingSwap!.IsReadyToActivate.Should().BeTrue();
        primary.PendingSwap.OverriddenBy.Should().Be(supervisor);
        primary.PendingSwap.PrimaryConsented.Should().BeFalse("an override is not a consent, and stays distinguishable");
    }

    /// <summary>TK-034: activation is refused while only one party has agreed.</summary>
    [Fact]
    public void ActivateSwap_Fails_WhenOnlyOnePartyHasConsented()
    {
        var primary = Assignment("emp-1");
        var secondary = Assignment("emp-2");
        primary.ProposeSwap(secondary.Id, "emp-1", "emp-2", Today, Guid.NewGuid());
        primary.ConsentToSwap("emp-1");

        primary.ActivateSwap(secondary.Id).Error.Should().Be(TimekeepingErrors.SwapRequiresBothPartiesConsent);
        primary.Status.Should().Be(ShiftAssignmentStatus.Scheduled);
    }

    [Fact]
    public void ActivateSwap_MovesThisSideAndLinksTheCounterpart_WhenBothConsented()
    {
        var primary = Assignment("emp-1");
        var secondary = Assignment("emp-2");
        primary.ProposeSwap(secondary.Id, "emp-1", "emp-2", Today, Guid.NewGuid());
        primary.ConsentToSwap("emp-1");
        primary.ConsentToSwap("emp-2");

        var result = primary.ActivateSwap(secondary.Id);

        result.IsSuccess.Should().BeTrue();
        primary.Status.Should().Be(ShiftAssignmentStatus.Swapped);
        primary.SwapLinkedAssignmentId.Should().Be(secondary.Id);
    }

    /// <summary>
    /// domain-events.md requires exactly one swap event carrying both sides, since two
    /// independent reassignment events would not say the two are related.
    /// </summary>
    [Fact]
    public void RecordSwapCompleted_RaisesOneEventCarryingBothSides_AndClearsTheProposal()
    {
        var primary = Assignment("emp-1");
        var secondary = Assignment("emp-2");
        primary.ProposeSwap(secondary.Id, "emp-1", "emp-2", Today, Guid.NewGuid());
        primary.ConsentToSwap("emp-1");
        primary.ConsentToSwap("emp-2");
        var pending = primary.PendingSwap!;
        primary.ActivateSwap(secondary.Id);
        secondary.ActivateSwap(primary.Id);

        primary.RecordSwapCompleted(secondary, pending, TestTimekeeping.NowUtc);

        var swapped = primary.DomainEvents.OfType<ShiftAssignmentSwapped>().Should().ContainSingle().Subject;
        swapped.PrimaryEmployeeId.Should().Be("emp-1");
        swapped.SecondaryEmployeeId.Should().Be("emp-2");
        swapped.BothPartiesConsented.Should().BeTrue();
        primary.PendingSwap.Should().BeNull();
    }
}
