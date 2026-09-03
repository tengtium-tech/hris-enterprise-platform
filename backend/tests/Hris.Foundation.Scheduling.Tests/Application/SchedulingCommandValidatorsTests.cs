using FluentAssertions;
using Hris.Foundation.Scheduling.Application.Commands;
using Hris.Foundation.Scheduling.Application.Queries;
using Hris.Foundation.Scheduling.Application.Validators;
using Hris.Foundation.Scheduling.Domain;
using Xunit;

namespace Hris.Foundation.Scheduling.Tests.Application;

/// <summary>
/// One valid-passes/invalid-fails pair per validator, the identical shape
/// <c>SearchCommandValidatorsTests</c> already establishes.
/// </summary>
public sealed class SchedulingCommandValidatorsTests
{
    [Fact]
    public void CreateScheduleCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyTenantId()
    {
        var validator = new CreateScheduleCommandValidator();
        var valid = new CreateScheduleCommand(Guid.NewGuid(), ScheduleType.CronBased, "0 0 * * *", "Asia/Manila", "PayrollProcessing", null, HolidayBehavior.ExecuteNormally, null);
        var invalid = valid with { TenantId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateScheduleCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new UpdateScheduleCommandValidator();
        var valid = new UpdateScheduleCommand(Guid.NewGuid(), "0 0 * * *", "Asia/Manila", "PayrollProcessing", null, HolidayBehavior.ExecuteNormally, null);
        var invalid = valid with { ScheduleId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateScheduleCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new ValidateScheduleCommandValidator();

        validator.Validate(new ValidateScheduleCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new ValidateScheduleCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ApproveScheduleCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new ApproveScheduleCommandValidator();

        validator.Validate(new ApproveScheduleCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new ApproveScheduleCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ActivateScheduleCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new ActivateScheduleCommandValidator();

        validator.Validate(new ActivateScheduleCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new ActivateScheduleCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void PauseScheduleCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new PauseScheduleCommandValidator();

        validator.Validate(new PauseScheduleCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new PauseScheduleCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ResumeScheduleCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new ResumeScheduleCommandValidator();

        validator.Validate(new ResumeScheduleCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new ResumeScheduleCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RetireScheduleCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new RetireScheduleCommandValidator();

        validator.Validate(new RetireScheduleCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new RetireScheduleCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void TriggerScheduleExecutionCommandValidator_AcceptsAValidCommand_AndRejectsANegativeRetryCount()
    {
        var validator = new TriggerScheduleExecutionCommandValidator();
        var valid = new TriggerScheduleExecutionCommand(Guid.NewGuid(), "job-0001", 0);
        var invalid = valid with { RetryCount = -1 };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CompleteScheduleExecutionCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new CompleteScheduleExecutionCommandValidator();

        validator.Validate(new CompleteScheduleExecutionCommand(Guid.NewGuid(), 1500)).IsValid.Should().BeTrue();
        validator.Validate(new CompleteScheduleExecutionCommand(Guid.Empty, 1500)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void FailScheduleExecutionCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyReason()
    {
        var validator = new FailScheduleExecutionCommandValidator();
        var valid = new FailScheduleExecutionCommand(Guid.NewGuid(), "Timed out", 5000);
        var invalid = valid with { Reason = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetScheduleQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyId()
    {
        var validator = new GetScheduleQueryValidator();

        validator.Validate(new GetScheduleQuery(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new GetScheduleQuery(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ListScheduleExecutionHistoryQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyTenantId()
    {
        var validator = new ListScheduleExecutionHistoryQueryValidator();
        var valid = new ListScheduleExecutionHistoryQuery(Guid.NewGuid(), Guid.NewGuid());
        var invalid = valid with { TenantId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }
}
