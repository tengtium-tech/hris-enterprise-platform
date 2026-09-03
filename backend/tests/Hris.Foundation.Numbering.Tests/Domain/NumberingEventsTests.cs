using FluentAssertions;
using Hris.Foundation.Numbering.Domain;
using Xunit;

namespace Hris.Foundation.Numbering.Tests.Domain;

/// <summary>
/// docs/09-testing/unit-and-integration-testing.md 2.2: "Equality is by value, not
/// reference." These eight records are Domain Events, not Value Objects, but the same
/// expectation applies to any immutable data-carrying type this framework hands to a
/// caller -- the identical shape FileStorageEventsTests already establishes.
/// </summary>
public sealed class NumberingEventsTests
{
    [Fact]
    public void NumberRequested_HasValueEquality_AndAUsefulToString()
    {
        var original = new NumberRequested(Guid.NewGuid(), TestData.NowUtc, new IssuedNumberId(Guid.NewGuid()), new NumberSeriesId(Guid.NewGuid()));
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(NumberRequested));
    }

    [Fact]
    public void NumberReserved_HasValueEquality_AndAUsefulToString()
    {
        var original = new NumberReserved(
            Guid.NewGuid(), TestData.NowUtc, new IssuedNumberId(Guid.NewGuid()), new NumberSeriesId(Guid.NewGuid()), 1, "EMP-2026-000001");
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(NumberReserved));
    }

    [Fact]
    public void NumberGenerated_HasValueEquality_AndAUsefulToString()
    {
        var original = new NumberGenerated(Guid.NewGuid(), TestData.NowUtc, new IssuedNumberId(Guid.NewGuid()), "EMP-2026-000001");
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(NumberGenerated));
    }

    [Fact]
    public void NumberAssigned_HasValueEquality_AndAUsefulToString()
    {
        var original = new NumberAssigned(Guid.NewGuid(), TestData.NowUtc, new IssuedNumberId(Guid.NewGuid()), "Employee", "EMP-0001");
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(NumberAssigned));
    }

    [Fact]
    public void NumberReleased_HasValueEquality_AndAUsefulToString()
    {
        var original = new NumberReleased(Guid.NewGuid(), TestData.NowUtc, new IssuedNumberId(Guid.NewGuid()), "Abandoned draft");
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(NumberReleased));
    }

    [Fact]
    public void NumberValidationFailed_HasValueEquality_AndAUsefulToString()
    {
        var original = new NumberValidationFailed(Guid.NewGuid(), TestData.NowUtc, new IssuedNumberId(Guid.NewGuid()));
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(NumberValidationFailed));
    }

    [Fact]
    public void SequenceReset_HasValueEquality_AndAUsefulToString()
    {
        var original = new SequenceReset(Guid.NewGuid(), TestData.NowUtc, new NumberSeriesId(Guid.NewGuid()));
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(SequenceReset));
    }

    [Fact]
    public void NumberSeriesUpdated_HasValueEquality_AndAUsefulToString()
    {
        var original = new NumberSeriesUpdated(Guid.NewGuid(), TestData.NowUtc, new NumberSeriesId(Guid.NewGuid()));
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(NumberSeriesUpdated));
    }
}
