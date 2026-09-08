using FluentAssertions;
using Hris.Modules.Employment.Domain;
using Xunit;

namespace Hris.Modules.Employment.Tests.Domain;

public sealed class ValueObjectTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmploymentNumber_Create_WithMissingValue_Fails(string? value)
    {
        var result = EmploymentNumber.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentNumberRequired);
    }

    [Fact]
    public void EmploymentNumber_Create_TooLong_Fails()
    {
        var result = EmploymentNumber.Create(new string('A', 51));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentNumberTooLong);
    }

    [Fact]
    public void EmploymentNumber_Create_NormalizesWhitespace()
    {
        var result = EmploymentNumber.Create("EMP   000001");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("EMP 000001");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void EmploymentType_Create_WithMissingValue_Fails(string? value)
    {
        var result = EmploymentType.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentTypeRequired);
    }

    [Fact]
    public void EmploymentType_Create_TooLong_Fails()
    {
        var result = EmploymentType.Create(new string('A', 101));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentTypeTooLong);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void EmploymentCategory_Create_WithMissingValue_Fails(string? value)
    {
        var result = EmploymentCategory.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentCategoryRequired);
    }

    [Fact]
    public void EmploymentCategory_Create_TooLong_Fails()
    {
        var result = EmploymentCategory.Create(new string('A', 101));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.EmploymentCategoryTooLong);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ContractType_Create_WithMissingValue_Fails(string? value)
    {
        var result = ContractType.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.ContractTypeRequired);
    }

    [Fact]
    public void ContractType_Create_TooLong_Fails()
    {
        var result = ContractType.Create(new string('A', 101));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.ContractTypeTooLong);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void TerminationReason_Create_WithMissingValue_Fails(string? value)
    {
        var result = TerminationReason.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.TerminationReasonRequired);
    }

    [Fact]
    public void TerminationReason_Create_TooLong_Fails()
    {
        var result = TerminationReason.Create(new string('A', 501));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.TerminationReasonTooLong);
    }

    [Fact]
    public void ContractPeriod_Create_OpenEnded_Succeeds()
    {
        var result = ContractPeriod.Create(new DateOnly(2026, 1, 1), null);

        result.IsSuccess.Should().BeTrue();
        result.Value.EndDate.Should().BeNull();
    }

    [Fact]
    public void ContractPeriod_Create_EndBeforeStart_Fails()
    {
        var result = ContractPeriod.Create(new DateOnly(2026, 1, 1), new DateOnly(2025, 12, 31));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.ContractPeriodEndBeforeStart);
    }

    [Fact]
    public void ContractPeriod_Contains_WithinOpenEndedRange_ReturnsTrue()
    {
        var period = ContractPeriod.Create(new DateOnly(2026, 1, 1), null).Value;

        period.Contains(new DateOnly(2030, 1, 1)).Should().BeTrue();
        period.Contains(new DateOnly(2025, 12, 31)).Should().BeFalse();
    }

    [Fact]
    public void ProbationDuration_Create_WithZeroDays_Fails()
    {
        var result = ProbationDuration.Create(0);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.ProbationDurationMustBePositive);
    }

    [Fact]
    public void ProbationDuration_ComputeEndDate_AddsDays()
    {
        var duration = ProbationDuration.Create(180).Value;

        duration.ComputeEndDate(new DateOnly(2026, 1, 1)).Should().Be(new DateOnly(2026, 1, 1).AddDays(180));
    }

    [Fact]
    public void CompensationAmount_Create_WithNegativeAmount_Fails()
    {
        var result = CompensationAmount.Create(-1m, "PHP", CompensationBasis.Monthly);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.CompensationAmountNegative);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("PH")]
    [InlineData("PHPX")]
    public void CompensationAmount_Create_WithInvalidCurrencyCode_Fails(string? currencyCode)
    {
        var result = CompensationAmount.Create(1000m, currencyCode, CompensationBasis.Monthly);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.CompensationCurrencyCodeInvalid);
    }

    [Fact]
    public void CompensationAmount_Create_NormalizesCurrencyCodeToUppercase()
    {
        var result = CompensationAmount.Create(1000m, "php", CompensationBasis.Monthly);

        result.IsSuccess.Should().BeTrue();
        result.Value.CurrencyCode.Should().Be("PHP");
    }
}
