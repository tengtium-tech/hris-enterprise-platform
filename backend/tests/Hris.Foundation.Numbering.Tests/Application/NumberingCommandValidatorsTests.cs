using FluentAssertions;
using Hris.Foundation.Numbering.Application.Commands;
using Hris.Foundation.Numbering.Application.Queries;
using Hris.Foundation.Numbering.Application.Validators;
using Hris.Foundation.Numbering.Domain;
using Xunit;

namespace Hris.Foundation.Numbering.Tests.Application;

/// <summary>
/// One valid-passes/invalid-fails pair per validator, the identical shape
/// <c>FileStorageCommandValidatorsTests</c> already establishes.
/// </summary>
public sealed class NumberingCommandValidatorsTests
{
    [Fact]
    public void RegisterNumberSeriesCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyKey()
    {
        var validator = new RegisterNumberSeriesCommandValidator();
        var valid = new RegisterNumberSeriesCommand("employee-numbers", "EMP", 6, true, false, "-", SequenceResetPolicy.Never);
        var invalid = valid with { Key = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateNumberSeriesFormatCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new UpdateNumberSeriesFormatCommandValidator();
        var valid = new UpdateNumberSeriesFormatCommand(Guid.NewGuid(), "EMP", 6, true, false, "-", SequenceResetPolicy.Never);
        var invalid = valid with { NumberSeriesId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ResetSequenceCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new ResetSequenceCommandValidator();

        validator.Validate(new ResetSequenceCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new ResetSequenceCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RequestAndReserveNumberCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new RequestAndReserveNumberCommandValidator();

        validator.Validate(new RequestAndReserveNumberCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new RequestAndReserveNumberCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ConfirmNumberGeneratedCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new ConfirmNumberGeneratedCommandValidator();

        validator.Validate(new ConfirmNumberGeneratedCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new ConfirmNumberGeneratedCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void AssignNumberCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyAssignedToType()
    {
        var validator = new AssignNumberCommandValidator();
        var valid = new AssignNumberCommand(Guid.NewGuid(), "Employee", "EMP-0001");
        var invalid = valid with { AssignedToType = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateNumberCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new ValidateNumberCommandValidator();

        validator.Validate(new ValidateNumberCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new ValidateNumberCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ReleaseNumberCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyReason()
    {
        var validator = new ReleaseNumberCommandValidator();
        var valid = new ReleaseNumberCommand(Guid.NewGuid(), "Abandoned");
        var invalid = valid with { Reason = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ArchiveNumberCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new ArchiveNumberCommandValidator();

        validator.Validate(new ArchiveNumberCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new ArchiveNumberCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetNumberSeriesQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyKey()
    {
        var validator = new GetNumberSeriesQueryValidator();

        validator.Validate(new GetNumberSeriesQuery("employee-numbers")).IsValid.Should().BeTrue();
        validator.Validate(new GetNumberSeriesQuery(string.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetIssuedNumberQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyId()
    {
        var validator = new GetIssuedNumberQueryValidator();

        validator.Validate(new GetIssuedNumberQuery(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new GetIssuedNumberQuery(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ListIssuedNumbersForSeriesQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyId()
    {
        var validator = new ListIssuedNumbersForSeriesQueryValidator();

        validator.Validate(new ListIssuedNumbersForSeriesQuery(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new ListIssuedNumbersForSeriesQuery(Guid.Empty)).IsValid.Should().BeFalse();
    }
}
