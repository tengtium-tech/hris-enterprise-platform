using FluentAssertions;
using Hris.Foundation.StatutoryReferenceData.Application.Commands;
using Hris.Foundation.StatutoryReferenceData.Application.Queries;
using Hris.Foundation.StatutoryReferenceData.Application.Validators;
using Hris.Foundation.StatutoryReferenceData.Domain;
using Xunit;

namespace Hris.Foundation.StatutoryReferenceData.Tests.Application;

/// <summary>
/// One valid-passes/invalid-fails pair per validator, the identical shape
/// <c>SchedulingCommandValidatorsTests</c> already establishes.
/// </summary>
public sealed class StatutoryReferenceDataCommandValidatorsTests
{
    [Fact]
    public void RegisterStatutoryProgramCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyCode()
    {
        var validator = new RegisterStatutoryProgramCommandValidator();
        var valid = new RegisterStatutoryProgramCommand("SSS", "PH", "SSS Contribution Schedule");
        var invalid = valid with { Code = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void PublishStatutoryTableVersionCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyProgramId()
    {
        var validator = new PublishStatutoryTableVersionCommandValidator();
        var valid = new PublishStatutoryTableVersionCommand(
            Guid.NewGuid(), "2025-01", TestData.NowUtc, null, "Social Security System (SSS)",
            "SSS Circular No. 2024-006", TestData.NowUtc, StatutoryVerificationSourceType.PrimarySourceRead,
            TestData.NowUtc, TestData.NewScheduleData());
        var invalid = valid with { StatutoryProgramId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RecordStatutoryTableVersionSignoffCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptySignoffBy()
    {
        var validator = new RecordStatutoryTableVersionSignoffCommandValidator();
        var valid = new RecordStatutoryTableVersionSignoffCommand(Guid.NewGuid(), "Reviewer Name");
        var invalid = valid with { SignoffBy = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetStatutoryProgramQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyId()
    {
        var validator = new GetStatutoryProgramQueryValidator();
        var valid = new GetStatutoryProgramQuery(Guid.NewGuid());
        var invalid = valid with { StatutoryProgramId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ListStatutoryProgramsQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyCountry()
    {
        var validator = new ListStatutoryProgramsQueryValidator();
        var valid = new ListStatutoryProgramsQuery("PH");
        var invalid = valid with { Country = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetEffectiveStatutoryTableVersionQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyProgramCode()
    {
        var validator = new GetEffectiveStatutoryTableVersionQueryValidator();
        var valid = new GetEffectiveStatutoryTableVersionQuery("SSS", "PH", TestData.NowUtc);
        var invalid = valid with { ProgramCode = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ListStatutoryTableVersionHistoryQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyProgramId()
    {
        var validator = new ListStatutoryTableVersionHistoryQueryValidator();
        var valid = new ListStatutoryTableVersionHistoryQuery(Guid.NewGuid());
        var invalid = valid with { StatutoryProgramId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }
}
