using Hris.Application.Abstractions;
using Hris.Foundation.Events.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Events.Application.Commands;

/// <summary>
/// Dead Letter Queue's own "Manual Recovery" capability. Unlike
/// <see cref="ReplayEventCommand"/>, this enforces its own narrower precondition --
/// the targeted entry must already be <see cref="OutboxEntryStatus.DeadLettered"/> --
/// since manual DLQ recovery is specifically about entries that exhausted retries, not
/// a general-purpose requeue. See <see cref="OutboxEntry.Requeue"/>'s own remarks for
/// why the underlying Domain method itself stays permissive and this precondition is
/// enforced here instead.
/// </summary>
public sealed record RequeueDeadLetterEventCommand(Guid OutboxEntryId) : ICommand<Result>;

internal sealed class RequeueDeadLetterEventCommandHandler : IRequestHandler<RequeueDeadLetterEventCommand, Result>
{
    private readonly IOutboxEntryRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RequeueDeadLetterEventCommandHandler(IOutboxEntryRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RequeueDeadLetterEventCommand request, CancellationToken cancellationToken)
    {
        var entry = await _repository
            .GetByIdAsync(new OutboxEntryId(request.OutboxEntryId), cancellationToken)
            .ConfigureAwait(false);

        if (entry is null)
        {
            return Result.Failure(EventErrors.OutboxEntryNotFound);
        }

        return entry.Status != OutboxEntryStatus.DeadLettered
            ? Result.Failure(EventErrors.OutboxEntryNotDeadLettered)
            : entry.Requeue(_timeProvider.GetUtcNow());
    }
}
