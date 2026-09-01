using Hris.Application.Abstractions;
using Hris.Foundation.Configuration.Application.Queries;
using Hris.Foundation.Configuration.Domain;
using Hris.Foundation.Events.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hris.Foundation.Events.Infrastructure.Publishing;

/// <summary>
/// The Background Publisher outbox-pattern.md's own section calls for: "A background
/// service is responsible for: Reading unpublished events, Publishing events, Marking
/// events as published, Retrying failed deliveries... Publishing must occur outside
/// the original business transaction."
///
/// Reads a batch of <see cref="OutboxEntryStatus.Pending"/> entries and marks each
/// <see cref="OutboxEntryStatus.Dispatched"/>. Deliberately does not yet invoke any
/// registered <see cref="IDomainEventSubscriber{TEvent}"/> for the entry's own event --
/// doing so requires resolving a concrete CLR <see cref="Type"/> from
/// <see cref="EventEnvelope.EventType"/>'s stored string and deserializing
/// <see cref="EventEnvelope.Payload"/> back into it, which in turn requires a
/// type-name-to-CLR-type registry that does not exist anywhere in this solution yet --
/// and, as of this Sprint, no framework or module registers a single
/// <see cref="IDomainEventSubscriber{TEvent}"/> implementation for this dispatcher to
/// invoke even if it could. Building that registry now, for zero current consumers,
/// would be exactly the speculative-design CLAUDE.md's own "Don't design for
/// hypothetical future requirements" warns against. An entry with no registered
/// consumer is still correctly "dispatched" in the meantime -- this framework's own
/// "Loose Coupling" principle means a publisher never depends on whether anyone is
/// listening. Add real subscriber invocation, and the retry/dead-letter path
/// <see cref="OutboxEntry.RecordFailedAttempt"/> already supports, once a first real
/// consumer exists to make that resolution concrete rather than speculative.
///
/// Uses <see cref="IServiceScopeFactory"/> to open a fresh DI scope per poll cycle --
/// this service is registered Singleton (the standard <see cref="BackgroundService"/>
/// lifetime), while <c>HrisDbContext</c> and every repository built on it are Scoped;
/// resolving them from the root container directly would either throw or, worse,
/// silently share one DbContext instance across the process's entire lifetime.
/// </summary>
public sealed class OutboxDispatcherBackgroundService : BackgroundService
{
    internal const string BatchSizeConfigurationKey = "Event.OutboxDispatchBatchSize";
    internal const string PollIntervalSecondsConfigurationKey = "Event.OutboxDispatchPollIntervalSeconds";

    private const int _defaultBatchSize = 50;
    private const int _defaultPollIntervalSeconds = 5;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;

    public OutboxDispatcherBackgroundService(IServiceScopeFactory scopeFactory, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var pollInterval = await DispatchOnceAsync(stoppingToken).ConfigureAwait(false);

            try
            {
                await Task.Delay(pollInterval, _timeProvider, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Requirement 6 (event-framework.md's own Transactional Publication
                // Requirements): shutdown here is a normal stop, not a failure --
                // undispatched entries stay Pending in the database and this loop
                // resumes from where it left off on the next process start.
            }
        }
    }

    private async Task<TimeSpan> DispatchOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var repository = scope.ServiceProvider.GetRequiredService<IOutboxEntryRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var nowUtc = _timeProvider.GetUtcNow();
        var asOfDate = DateOnly.FromDateTime(nowUtc.UtcDateTime);

        var batchSize = await ResolveIntSettingAsync(sender, BatchSizeConfigurationKey, _defaultBatchSize, asOfDate, cancellationToken)
            .ConfigureAwait(false);
        var pollIntervalSeconds = await ResolveIntSettingAsync(
            sender, PollIntervalSecondsConfigurationKey, _defaultPollIntervalSeconds, asOfDate, cancellationToken)
            .ConfigureAwait(false);

        var pending = await repository.GetPendingBatchAsync(batchSize, cancellationToken).ConfigureAwait(false);

        foreach (var entry in pending)
        {
            entry.MarkDispatched(_timeProvider.GetUtcNow());
        }

        if (pending.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return TimeSpan.FromSeconds(pollIntervalSeconds);
    }

    private static async Task<int> ResolveIntSettingAsync(
        ISender sender, string key, int defaultValue, DateOnly asOfDate, CancellationToken cancellationToken)
    {
        var query = new ResolveConfigurationValueQuery(key, [ConfigurationScope.Global()], asOfDate);
        var result = await sender.Send(query, cancellationToken).ConfigureAwait(false);

        // The identical "absence of an operator override resolves to a safe default,
        // never a thrown error" reasoning LoggingService.ResolveMinimumSeverityAsync
        // and AuthenticateCommandHandler.ResolveIntSettingAsync already document: outbox
        // dispatch must keep running even when no policy override has ever been
        // configured.
        return result.IsSuccess && int.TryParse(result.Value, out var parsed) && parsed > 0
            ? parsed
            : defaultValue;
    }
}
