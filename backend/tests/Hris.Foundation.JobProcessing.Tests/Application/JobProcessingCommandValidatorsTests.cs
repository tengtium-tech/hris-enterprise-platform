using FluentAssertions;
using Hris.Foundation.JobProcessing.Application.Commands;
using Hris.Foundation.JobProcessing.Application.Queries;
using Hris.Foundation.JobProcessing.Application.Validators;
using Hris.Foundation.JobProcessing.Domain;
using Xunit;

namespace Hris.Foundation.JobProcessing.Tests.Application;

/// <summary>
/// One valid-passes/invalid-fails pair per validator, the identical shape
/// <c>SchedulingCommandValidatorsTests</c> already establishes.
/// </summary>
public sealed class JobProcessingCommandValidatorsTests
{
    [Fact]
    public void SubmitJobCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyJobType()
    {
        var validator = new SubmitJobCommandValidator();
        var valid = new SubmitJobCommand(Guid.NewGuid(), "PayrollCalculation", "PayrollQueue", JobPriority.Normal, null, null, null);
        var invalid = valid with { JobType = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void EnqueueJobCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new EnqueueJobCommandValidator();

        validator.Validate(new EnqueueJobCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new EnqueueJobCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void MarkJobScheduledCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new MarkJobScheduledCommandValidator();

        validator.Validate(new MarkJobScheduledCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new MarkJobScheduledCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void StartJobCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new StartJobCommandValidator();

        validator.Validate(new StartJobCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new StartJobCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CompleteJobCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new CompleteJobCommandValidator();

        validator.Validate(new CompleteJobCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new CompleteJobCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void FailJobCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyReason()
    {
        var validator = new FailJobCommandValidator();
        var valid = new FailJobCommand(Guid.NewGuid(), "Timed out.");
        var invalid = valid with { Reason = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RetryJobCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new RetryJobCommandValidator();

        validator.Validate(new RetryJobCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new RetryJobCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void MoveJobToDeadLetterQueueCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyReason()
    {
        var validator = new MoveJobToDeadLetterQueueCommandValidator();
        var valid = new MoveJobToDeadLetterQueueCommand(Guid.NewGuid(), "Exceeded retry limit.");
        var invalid = valid with { Reason = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CancelJobCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new CancelJobCommandValidator();

        validator.Validate(new CancelJobCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new CancelJobCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RegisterJobQueueCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyName()
    {
        var validator = new RegisterJobQueueCommandValidator();
        var valid = new RegisterJobQueueCommand("PayrollQueue", 5, 3, 60);
        var invalid = valid with { Name = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateJobQueuePolicyCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new UpdateJobQueuePolicyCommandValidator();
        var valid = new UpdateJobQueuePolicyCommand(Guid.NewGuid(), 5, 3, 60);
        var invalid = valid with { JobQueueId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void StartWorkerCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyInstanceId()
    {
        var validator = new StartWorkerCommandValidator();
        var valid = new StartWorkerCommand("worker-0001");
        var invalid = valid with { InstanceId = string.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void StopWorkerCommandValidator_AcceptsAValidCommand_AndRejectsAnEmptyId()
    {
        var validator = new StopWorkerCommandValidator();

        validator.Validate(new StopWorkerCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new StopWorkerCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetJobQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyId()
    {
        var validator = new GetJobQueryValidator();

        validator.Validate(new GetJobQuery(Guid.NewGuid())).IsValid.Should().BeTrue();
        validator.Validate(new GetJobQuery(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ListJobHistoryQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyTenantId()
    {
        var validator = new ListJobHistoryQueryValidator();
        var valid = new ListJobHistoryQuery(Guid.NewGuid(), Guid.NewGuid());
        var invalid = valid with { TenantId = Guid.Empty };

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(invalid).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetJobQueueQueryValidator_AcceptsAValidQuery_AndRejectsAnEmptyName()
    {
        var validator = new GetJobQueueQueryValidator();

        validator.Validate(new GetJobQueueQuery("PayrollQueue")).IsValid.Should().BeTrue();
        validator.Validate(new GetJobQueueQuery(string.Empty)).IsValid.Should().BeFalse();
    }
}
