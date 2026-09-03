using FluentAssertions;
using Hris.Foundation.Numbering.Domain;
using Xunit;

namespace Hris.Foundation.Numbering.Tests.Domain;

public sealed class IssuedNumberTests
{
    [Fact]
    public void Request_Succeeds_AndRaisesNumberRequested()
    {
        var seriesId = new NumberSeriesId(Guid.NewGuid());

        var result = IssuedNumber.Request(seriesId, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.NumberSeriesId.Should().Be(seriesId);
        result.Value.Status.Should().Be(NumberLifecycleStatus.Requested);
        result.Value.SequenceValue.Should().BeNull();
        result.Value.FormattedNumber.Should().BeNull();
        result.Value.DomainEvents.OfType<NumberRequested>().Should().ContainSingle();
    }

    [Fact]
    public void Reserve_Succeeds_FromRequested_AndRaisesNumberReserved()
    {
        var issuedNumber = TestData.RequestedNumber();
        var formattedNumber = FormattedNumber.Create("EMP-2026-000042").Value;

        var result = issuedNumber.Reserve(42, formattedNumber, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        issuedNumber.Status.Should().Be(NumberLifecycleStatus.Reserved);
        issuedNumber.SequenceValue.Should().Be(42);
        issuedNumber.FormattedNumber.Should().Be(formattedNumber);
        issuedNumber.IssuedAtUtc.Should().Be(TestData.NowUtc);
        issuedNumber.DomainEvents.OfType<NumberReserved>().Should().ContainSingle()
            .Which.SequenceValue.Should().Be(42);
    }

    [Fact]
    public void Reserve_Fails_WhenNotRequested()
    {
        var issuedNumber = TestData.ReservedNumber();

        var result = issuedNumber.Reserve(2, FormattedNumber.Create("EMP-2026-000002").Value, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.InvalidNumberLifecycleTransition);
    }

    [Fact]
    public void MarkGenerated_Succeeds_FromReserved_AndRaisesNumberGenerated()
    {
        var issuedNumber = TestData.ReservedNumber();

        var result = issuedNumber.MarkGenerated(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        issuedNumber.Status.Should().Be(NumberLifecycleStatus.Generated);
        issuedNumber.DomainEvents.OfType<NumberGenerated>().Should().ContainSingle();
    }

    [Fact]
    public void MarkGenerated_Fails_WhenNotReserved()
    {
        var issuedNumber = TestData.RequestedNumber();

        var result = issuedNumber.MarkGenerated(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.InvalidNumberLifecycleTransition);
    }

    [Fact]
    public void Assign_Succeeds_FromGenerated_AndRaisesNumberAssigned()
    {
        var issuedNumber = TestData.GeneratedNumber();

        var result = issuedNumber.Assign("Employee", "EMP-0001", TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        issuedNumber.Status.Should().Be(NumberLifecycleStatus.Assigned);
        issuedNumber.AssignedToType.Should().Be("Employee");
        issuedNumber.AssignedToReferenceId.Should().Be("EMP-0001");
        issuedNumber.DomainEvents.OfType<NumberAssigned>().Should().ContainSingle();
    }

    [Fact]
    public void Assign_Fails_WhenNotGenerated()
    {
        var issuedNumber = TestData.ReservedNumber();

        var result = issuedNumber.Assign("Employee", "EMP-0001", TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.InvalidNumberLifecycleTransition);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Assign_Fails_WhenAssignedToTypeIsNullOrWhitespace(string? assignedToType)
    {
        var issuedNumber = TestData.GeneratedNumber();

        var result = issuedNumber.Assign(assignedToType, "EMP-0001", TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.AssignedToTypeRequired);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Assign_Fails_WhenAssignedToReferenceIdIsNullOrWhitespace(string? assignedToReferenceId)
    {
        var issuedNumber = TestData.GeneratedNumber();

        var result = issuedNumber.Assign("Employee", assignedToReferenceId, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.AssignedToReferenceIdRequired);
    }

    [Fact]
    public void Validate_Succeeds_WhenFormattedNumberStillMatchesCurrentSeriesRules_AndRaisesNoEvent()
    {
        var prefix = TestData.NewPrefix("EMP");
        var format = TestData.NewFormat(includeYear: true);
        var formattedNumber = FormattedNumber.Create(format.Format(prefix, 42, TestData.NowUtc)).Value;
        var issuedNumber = TestData.AssignedNumber(sequenceValue: 42, formattedNumber: formattedNumber, nowUtc: TestData.NowUtc);

        var result = issuedNumber.Validate(prefix, format, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        issuedNumber.Status.Should().Be(NumberLifecycleStatus.Validated);
        issuedNumber.DomainEvents.OfType<NumberValidationFailed>().Should().BeEmpty();
    }

    [Fact]
    public void Validate_Fails_WhenSeriesFormatChangedSinceIssuance_AndRaisesNumberValidationFailed()
    {
        var originalPrefix = TestData.NewPrefix("EMP");
        var originalFormat = TestData.NewFormat(includeYear: true);
        var formattedNumber = FormattedNumber.Create(originalFormat.Format(originalPrefix, 42, TestData.NowUtc)).Value;
        var issuedNumber = TestData.AssignedNumber(sequenceValue: 42, formattedNumber: formattedNumber, nowUtc: TestData.NowUtc);

        // The series' own format was updated after this number was issued -- a real,
        // meaningfully-failable drift, not a synthetic always-true check.
        var updatedPrefix = TestData.NewPrefix("EEE");

        var result = issuedNumber.Validate(updatedPrefix, originalFormat, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.NumberFormatMismatch);
        issuedNumber.Status.Should().Be(NumberLifecycleStatus.Assigned, "a failed re-check does not silently advance the lifecycle");
        issuedNumber.DomainEvents.OfType<NumberValidationFailed>().Should().ContainSingle();
    }

    [Fact]
    public void Validate_Fails_WhenNotAssigned()
    {
        var issuedNumber = TestData.GeneratedNumber();

        var result = issuedNumber.Validate(TestData.NewPrefix(), TestData.NewFormat(), TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.InvalidNumberLifecycleTransition);
    }

    [Theory]
    [InlineData(NumberLifecycleStatus.Requested)]
    [InlineData(NumberLifecycleStatus.Reserved)]
    [InlineData(NumberLifecycleStatus.Generated)]
    public void Release_Succeeds_FromAnyPreAssignmentStatus(NumberLifecycleStatus status)
    {
        var issuedNumber = status switch
        {
            NumberLifecycleStatus.Requested => TestData.RequestedNumber(),
            NumberLifecycleStatus.Reserved => TestData.ReservedNumber(),
            _ => TestData.GeneratedNumber(),
        };

        var result = issuedNumber.Release("Draft abandoned", TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        issuedNumber.Status.Should().Be(NumberLifecycleStatus.Released);
        issuedNumber.DomainEvents.OfType<NumberReleased>().Should().ContainSingle()
            .Which.Reason.Should().Be("Draft abandoned");
    }

    [Fact]
    public void Release_Fails_WhenAlreadyAssigned()
    {
        var issuedNumber = TestData.AssignedNumber();

        var result = issuedNumber.Release("Too late", TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.InvalidNumberLifecycleTransition);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Release_Fails_WhenReasonIsNullOrWhitespace(string? reason)
    {
        var issuedNumber = TestData.RequestedNumber();

        var result = issuedNumber.Release(reason, TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.ReleaseReasonRequired);
    }

    [Fact]
    public void Archive_Succeeds_FromValidated()
    {
        var prefix = TestData.NewPrefix("EMP");
        var format = TestData.NewFormat();
        var formattedNumber = FormattedNumber.Create(format.Format(prefix, 1, TestData.NowUtc)).Value;
        var issuedNumber = TestData.AssignedNumber(sequenceValue: 1, formattedNumber: formattedNumber, nowUtc: TestData.NowUtc);
        issuedNumber.Validate(prefix, format, TestData.NowUtc);

        var result = issuedNumber.Archive();

        result.IsSuccess.Should().BeTrue();
        issuedNumber.Status.Should().Be(NumberLifecycleStatus.Archived);
    }

    [Fact]
    public void Archive_Fails_WhenNotValidated()
    {
        var issuedNumber = TestData.AssignedNumber();

        var result = issuedNumber.Archive();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NumberingErrors.InvalidNumberLifecycleTransition);
    }
}
