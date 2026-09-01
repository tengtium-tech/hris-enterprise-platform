using FluentValidation;
using Hris.Foundation.Events.Application.Commands;

namespace Hris.Foundation.Events.Application.Validators;

/// <summary>
/// application-pipeline.md's Validation Behavior scope: "Required fields... Business-
/// independent validation." Deliberately does not re-check anything
/// <see cref="Domain.OutboxEntry"/>'s own transition methods already enforce (status
/// preconditions) -- the identical separation <c>ConfigurationCommandValidators</c> and
/// <c>IdentityCommandValidators</c> state for their own commands.
/// </summary>
public sealed class ReplayEventCommandValidator : AbstractValidator<ReplayEventCommand>
{
    public ReplayEventCommandValidator()
    {
        RuleFor(c => c.OutboxEntryId).NotEmpty();
    }
}

public sealed class RequeueDeadLetterEventCommandValidator : AbstractValidator<RequeueDeadLetterEventCommand>
{
    public RequeueDeadLetterEventCommandValidator()
    {
        RuleFor(c => c.OutboxEntryId).NotEmpty();
    }
}
