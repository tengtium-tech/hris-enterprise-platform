using Hris.Application.Abstractions;
using Hris.Foundation.Events.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Events.Application.Commands;

/// <summary>
/// Event Replay's own scope item: "The framework should support replaying historical
/// events for: Recovery, Testing, Data Synchronization, Analytics, Audit... Replay
/// operations should be controlled and auditable." Accepts an entry in any
/// <see cref="OutboxEntryStatus"/>, including an already-<see cref="OutboxEntryStatus.Dispatched"/>
/// one -- unlike <see cref="RequeueDeadLetterEventCommand"/>, replay is not recovery
/// from failure; re-queuing something that already succeeded, on purpose, for testing
/// or audit, is exactly what this section names.
///
/// "Auditable" is not yet enforced here: Audit Framework has no Infrastructure layer
/// yet (it is built after Event Framework in Sprint 3's bootstrap order per
/// IMPLEMENTATION-PLAN.md's own dependency-cycle finding). Once it does, this handler
/// should record who replayed which entry and when -- not invented as a placeholder
/// audit call now.
/// </summary>
public sealed record ReplayEventCommand(Guid OutboxEntryId) : ICommand<Result>;

internal sealed class ReplayEventCommandHandler : IRequestHandler<ReplayEventCommand, Result>
{
    private readonly IOutboxEntryRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ReplayEventCommandHandler(IOutboxEntryRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ReplayEventCommand request, CancellationToken cancellationToken)
    {
        var entry = await _repository
            .GetByIdAsync(new OutboxEntryId(request.OutboxEntryId), cancellationToken)
            .ConfigureAwait(false);

        return entry is null
            ? Result.Failure(EventErrors.OutboxEntryNotFound)
            : entry.Requeue(_timeProvider.GetUtcNow());
    }
}
