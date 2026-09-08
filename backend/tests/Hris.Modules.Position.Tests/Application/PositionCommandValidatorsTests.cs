using FluentAssertions;
using Hris.Modules.Position.Application.Commands;
using Hris.Modules.Position.Application.Validators;
using Xunit;

namespace Hris.Modules.Position.Tests.Application;

/// <summary>
/// One valid-passes/invalid-fails pair per validator, the identical shape
/// <c>OrganizationCommandValidatorsTests</c> already establishes.
/// </summary>
public sealed class PositionCommandValidatorsTests
{
    [Fact]
    public void CreatePositionCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyNumber()
    {
        var validator = new CreatePositionCommandValidator();
        var valid = new CreatePositionCommand(
            Guid.NewGuid(), "POS-000001", "Title", "Type", Guid.NewGuid(), null, null, null, null, null, null, null,
            null, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 1);

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { Number = string.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreatePositionCommandValidator_Rejects_NegativeAuthorizedHeadcount()
    {
        var validator = new CreatePositionCommandValidator();
        var valid = new CreatePositionCommand(
            Guid.NewGuid(), "POS-000001", "Title", "Type", Guid.NewGuid(), null, null, null, null, null, null, null,
            null, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 1);

        validator.Validate(valid with { AuthorizedHeadcount = -1 }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdatePositionCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyPositionId()
    {
        var validator = new UpdatePositionCommandValidator();
        var valid = new UpdatePositionCommand(
            Guid.NewGuid(), Guid.NewGuid(), "Title", "Type", Guid.NewGuid(), null, null, null, null, null, null, null,
            null, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { PositionId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ActivatePositionCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyTenantId()
    {
        var validator = new ActivatePositionCommandValidator();
        var valid = new ActivatePositionCommand(Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { TenantId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void DeactivatePositionCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyPositionId()
    {
        var validator = new DeactivatePositionCommandValidator();
        var valid = new DeactivatePositionCommand(Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { PositionId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ArchivePositionCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyTenantId()
    {
        var validator = new ArchivePositionCommandValidator();
        var valid = new ArchivePositionCommand(Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { TenantId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void AssignReportingPositionCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyReportingPositionId()
    {
        var validator = new AssignReportingPositionCommandValidator();
        var valid = new AssignReportingPositionCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { ReportingPositionId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RemoveReportingPositionCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyPositionId()
    {
        var validator = new RemoveReportingPositionCommandValidator();
        var valid = new RemoveReportingPositionCommand(Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { PositionId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateAuthorizedHeadcountCommandValidator_AcceptsAValidCommand_AndRejectsNegativeHeadcount()
    {
        var validator = new UpdateAuthorizedHeadcountCommandValidator();
        var valid = new UpdateAuthorizedHeadcountCommand(Guid.NewGuid(), Guid.NewGuid(), 5);

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { AuthorizedHeadcount = -1 }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void MarkPositionVacantCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyPositionId()
    {
        var validator = new MarkPositionVacantCommandValidator();
        var valid = new MarkPositionVacantCommand(Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { PositionId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void MarkPositionFilledCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyPositionId()
    {
        var validator = new MarkPositionFilledCommandValidator();
        var valid = new MarkPositionFilledCommand(Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { PositionId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateJobFamilyCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyCode()
    {
        var validator = new CreateJobFamilyCommandValidator();
        var valid = new CreateJobFamilyCommand(Guid.NewGuid(), "IT", "Information Technology", null);

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { Code = string.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateJobFamilyCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyJobFamilyId()
    {
        var validator = new UpdateJobFamilyCommandValidator();
        var valid = new UpdateJobFamilyCommand(Guid.NewGuid(), Guid.NewGuid(), "New Name", null);

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { JobFamilyId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ActivateJobFamilyCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyTenantId()
    {
        var validator = new ActivateJobFamilyCommandValidator();
        var valid = new ActivateJobFamilyCommand(Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { TenantId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void DeactivateJobFamilyCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyJobFamilyId()
    {
        var validator = new DeactivateJobFamilyCommandValidator();
        var valid = new DeactivateJobFamilyCommand(Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { JobFamilyId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ArchiveJobFamilyCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyTenantId()
    {
        var validator = new ArchiveJobFamilyCommandValidator();
        var valid = new ArchiveJobFamilyCommand(Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { TenantId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateJobClassificationCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyName()
    {
        var validator = new CreateJobClassificationCommandValidator();
        var valid = new CreateJobClassificationCommand(Guid.NewGuid(), "PROF", "Professional", null);

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { Name = string.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateJobClassificationCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyJobClassificationId()
    {
        var validator = new UpdateJobClassificationCommandValidator();
        var valid = new UpdateJobClassificationCommand(Guid.NewGuid(), Guid.NewGuid(), "New Name", null);

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { JobClassificationId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ActivateJobClassificationCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyTenantId()
    {
        var validator = new ActivateJobClassificationCommandValidator();
        var valid = new ActivateJobClassificationCommand(Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { TenantId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void DeactivateJobClassificationCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyJobClassificationId()
    {
        var validator = new DeactivateJobClassificationCommandValidator();
        var valid = new DeactivateJobClassificationCommand(Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { JobClassificationId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ArchiveJobClassificationCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyTenantId()
    {
        var validator = new ArchiveJobClassificationCommandValidator();
        var valid = new ArchiveJobClassificationCommand(Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { TenantId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateJobGradeCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyCode()
    {
        var validator = new CreateJobGradeCommandValidator();
        var valid = new CreateJobGradeCommand(Guid.NewGuid(), "G08", "Grade 8", null, 8);

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { Code = string.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateJobGradeCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyJobGradeId()
    {
        var validator = new UpdateJobGradeCommandValidator();
        var valid = new UpdateJobGradeCommand(Guid.NewGuid(), Guid.NewGuid(), "Grade 9", null, 9);

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { JobGradeId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ActivateJobGradeCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyTenantId()
    {
        var validator = new ActivateJobGradeCommandValidator();
        var valid = new ActivateJobGradeCommand(Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { TenantId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void DeactivateJobGradeCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyJobGradeId()
    {
        var validator = new DeactivateJobGradeCommandValidator();
        var valid = new DeactivateJobGradeCommand(Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { JobGradeId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ArchiveJobGradeCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyTenantId()
    {
        var validator = new ArchiveJobGradeCommandValidator();
        var valid = new ArchiveJobGradeCommand(Guid.NewGuid(), Guid.NewGuid());

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { TenantId = Guid.Empty }).IsValid.Should().BeFalse();
    }
}
