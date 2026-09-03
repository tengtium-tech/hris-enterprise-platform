using FluentValidation;
using Hris.Foundation.JobProcessing.Application.Commands;
using Hris.Foundation.JobProcessing.Application.Queries;

namespace Hris.Foundation.JobProcessing.Application.Validators;

/// <summary>
/// application-pipeline.md's Validation Behavior scope: "Required fields...
/// Business-independent validation." Deliberately does not re-check anything the
/// Domain layer's own factory/transition methods already enforce (queue-name shape,
/// policy ranges, lifecycle-state gating) -- the identical separation every other
/// framework's own validators file states for its own set.
/// </summary>
public sealed class SubmitJobCommandValidator : AbstractValidator<SubmitJobCommand>
{
    public SubmitJobCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.JobType).NotEmpty();
        RuleFor(c => c.QueueName).NotEmpty();
    }
}

public sealed class EnqueueJobCommandValidator : AbstractValidator<EnqueueJobCommand>
{
    public EnqueueJobCommandValidator()
    {
        RuleFor(c => c.JobId).NotEmpty();
    }
}

public sealed class MarkJobScheduledCommandValidator : AbstractValidator<MarkJobScheduledCommand>
{
    public MarkJobScheduledCommandValidator()
    {
        RuleFor(c => c.JobId).NotEmpty();
    }
}

public sealed class StartJobCommandValidator : AbstractValidator<StartJobCommand>
{
    public StartJobCommandValidator()
    {
        RuleFor(c => c.JobId).NotEmpty();
    }
}

public sealed class CompleteJobCommandValidator : AbstractValidator<CompleteJobCommand>
{
    public CompleteJobCommandValidator()
    {
        RuleFor(c => c.JobId).NotEmpty();
    }
}

public sealed class FailJobCommandValidator : AbstractValidator<FailJobCommand>
{
    public FailJobCommandValidator()
    {
        RuleFor(c => c.JobId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
    }
}

public sealed class RetryJobCommandValidator : AbstractValidator<RetryJobCommand>
{
    public RetryJobCommandValidator()
    {
        RuleFor(c => c.JobId).NotEmpty();
    }
}

public sealed class MoveJobToDeadLetterQueueCommandValidator : AbstractValidator<MoveJobToDeadLetterQueueCommand>
{
    public MoveJobToDeadLetterQueueCommandValidator()
    {
        RuleFor(c => c.JobId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
    }
}

public sealed class CancelJobCommandValidator : AbstractValidator<CancelJobCommand>
{
    public CancelJobCommandValidator()
    {
        RuleFor(c => c.JobId).NotEmpty();
    }
}

public sealed class RegisterJobQueueCommandValidator : AbstractValidator<RegisterJobQueueCommand>
{
    public RegisterJobQueueCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty();
    }
}

public sealed class UpdateJobQueuePolicyCommandValidator : AbstractValidator<UpdateJobQueuePolicyCommand>
{
    public UpdateJobQueuePolicyCommandValidator()
    {
        RuleFor(c => c.JobQueueId).NotEmpty();
    }
}

public sealed class StartWorkerCommandValidator : AbstractValidator<StartWorkerCommand>
{
    public StartWorkerCommandValidator()
    {
        RuleFor(c => c.InstanceId).NotEmpty();
    }
}

public sealed class StopWorkerCommandValidator : AbstractValidator<StopWorkerCommand>
{
    public StopWorkerCommandValidator()
    {
        RuleFor(c => c.WorkerId).NotEmpty();
    }
}

public sealed class GetJobQueryValidator : AbstractValidator<GetJobQuery>
{
    public GetJobQueryValidator()
    {
        RuleFor(q => q.JobId).NotEmpty();
    }
}

public sealed class ListJobHistoryQueryValidator : AbstractValidator<ListJobHistoryQuery>
{
    public ListJobHistoryQueryValidator()
    {
        RuleFor(q => q.JobQueueId).NotEmpty();
        RuleFor(q => q.TenantId).NotEmpty();
    }
}

public sealed class GetJobQueueQueryValidator : AbstractValidator<GetJobQueueQuery>
{
    public GetJobQueueQueryValidator()
    {
        RuleFor(q => q.Name).NotEmpty();
    }
}
