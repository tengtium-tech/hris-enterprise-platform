using FluentAssertions;
using Hris.Modules.Position.Domain;
using Xunit;

namespace Hris.Modules.Position.Tests.Domain;

public sealed class ValueObjectTests
{
    [Fact]
    public void PositionNumber_Create_NormalizesToUpperInvariant()
    {
        var result = PositionNumber.Create("pos-000001");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("POS-000001");
    }

    [Fact]
    public void PositionNumber_Create_Fails_WhenMissing()
    {
        var result = PositionNumber.Create(null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.PositionNumberRequired);
    }

    [Fact]
    public void PositionNumber_Create_Fails_WhenTooLong()
    {
        var result = PositionNumber.Create(new string('A', 51));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.PositionNumberTooLong);
    }

    [Fact]
    public void PositionTitle_Create_NormalizesWhitespace()
    {
        var result = PositionTitle.Create("Senior   Software    Engineer");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("Senior Software Engineer");
    }

    [Fact]
    public void PositionTitle_Create_Fails_WhenMissing()
    {
        var result = PositionTitle.Create(" ");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.PositionTitleRequired);
    }

    [Fact]
    public void PositionTitle_Create_Fails_WhenTooLong()
    {
        var result = PositionTitle.Create(new string('A', 201));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.PositionTitleTooLong);
    }

    [Fact]
    public void PositionType_Create_Fails_WhenMissing()
    {
        var result = PositionType.Create(null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.PositionTypeRequired);
    }

    [Fact]
    public void PositionType_Create_Fails_WhenTooLong()
    {
        var result = PositionType.Create(new string('A', 101));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.PositionTypeTooLong);
    }

    [Fact]
    public void JobFamilyCode_Create_Fails_WhenTooLong()
    {
        var result = JobFamilyCode.Create(new string('A', 21));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.JobFamilyCodeTooLong);
    }

    [Fact]
    public void JobFamilyName_Create_Fails_WhenTooLong()
    {
        var result = JobFamilyName.Create(new string('A', 201));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.JobFamilyNameTooLong);
    }

    [Fact]
    public void JobClassificationCode_Create_Fails_WhenTooLong()
    {
        var result = JobClassificationCode.Create(new string('A', 21));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.JobClassificationCodeTooLong);
    }

    [Fact]
    public void JobClassificationName_Create_Fails_WhenTooLong()
    {
        var result = JobClassificationName.Create(new string('A', 201));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.JobClassificationNameTooLong);
    }

    [Fact]
    public void JobGradeCode_Create_Fails_WhenTooLong()
    {
        var result = JobGradeCode.Create(new string('A', 21));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.JobGradeCodeTooLong);
    }

    [Fact]
    public void JobGradeName_Create_Fails_WhenTooLong()
    {
        var result = JobGradeName.Create(new string('A', 201));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.JobGradeNameTooLong);
    }

    [Fact]
    public void AuthorizedHeadcount_Create_Succeeds_WithZero()
    {
        var result = AuthorizedHeadcount.Create(0);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(0);
    }

    [Fact]
    public void AuthorizedHeadcount_Create_Fails_WhenNegative()
    {
        var result = AuthorizedHeadcount.Create(-1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.AuthorizedHeadcountNegative);
    }

    [Fact]
    public void ValueObjects_AreEqual_ByValue()
    {
        PositionNumber.Create("POS-1").Value.Should().Be(PositionNumber.Create("pos-1").Value);
        JobFamilyCode.Create("IT").Value.Should().Be(JobFamilyCode.Create("it").Value);
    }
}
