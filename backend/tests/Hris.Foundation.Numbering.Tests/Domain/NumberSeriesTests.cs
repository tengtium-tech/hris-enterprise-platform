using FluentAssertions;
using Hris.Foundation.Numbering.Domain;
using Xunit;

namespace Hris.Foundation.Numbering.Tests.Domain;

public sealed class NumberSeriesTests
{
    [Fact]
    public void Register_Succeeds_WithValidInput()
    {
        var key = TestData.NewSeriesKey("employee-numbers");
        var prefix = TestData.NewPrefix("EMP");
        var format = TestData.NewFormat();

        var result = NumberSeries.Register(key, prefix, format, SequenceResetPolicy.Annual);

        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Should().Be(key);
        result.Value.Prefix.Should().Be(prefix);
        result.Value.Format.Should().Be(format);
        result.Value.ResetPolicy.Should().Be(SequenceResetPolicy.Annual);
        result.Value.CurrentSequenceValue.Should().Be(0);
        result.Value.LastResetAtUtc.Should().BeNull();
    }

    [Fact]
    public void UpdateFormat_Succeeds_AndRaisesNumberSeriesUpdated()
    {
        var series = TestData.RegisteredSeries();
        var newPrefix = TestData.NewPrefix("EEE");
        var newFormat = TestData.NewFormat(runningNumberLength: 8);

        var result = series.UpdateFormat(newPrefix, newFormat, SequenceResetPolicy.Monthly, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        series.Prefix.Should().Be(newPrefix);
        series.Format.Should().Be(newFormat);
        series.ResetPolicy.Should().Be(SequenceResetPolicy.Monthly);
        series.DomainEvents.OfType<NumberSeriesUpdated>().Should().ContainSingle();
    }

    [Fact]
    public void ResetSequence_Succeeds_RegardlessOfResetPolicy_AndRaisesSequenceReset()
    {
        var series = TestData.SeriesWithSequenceValue(42);

        var result = series.ResetSequence(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        series.CurrentSequenceValue.Should().Be(0);
        series.LastResetAtUtc.Should().Be(TestData.NowUtc);
        series.DomainEvents.OfType<SequenceReset>().Should().ContainSingle();
    }

    [Fact]
    public void ReconcileSequenceValueAfterAtomicIncrement_UpdatesInMemoryValue_WithoutRaisingAnEvent()
    {
        var series = TestData.RegisteredSeries();

        series.ReconcileSequenceValueAfterAtomicIncrement(7);

        series.CurrentSequenceValue.Should().Be(7);
        series.DomainEvents.Should().BeEmpty("this only records an increment that already happened elsewhere, it is not itself a business event");
    }
}
