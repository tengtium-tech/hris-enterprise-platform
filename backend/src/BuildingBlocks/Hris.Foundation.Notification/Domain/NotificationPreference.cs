using Hris.SharedKernel;

namespace Hris.Foundation.Notification.Domain;

/// <summary>
/// Aggregate Root holding one user's own notification settings within one tenant, per
/// notification-framework.md's own User Preferences section: "Preferred Language,
/// Preferred Channels, Notification Frequency, Quiet Hours, Digest Mode, Opt-In/Opt-Out
/// Preferences." A config aggregate, not population-scale -- one row per (tenant, user)
/// pair, applied by a future delivery worker per this document's own AI Implementation
/// Guidance ("Apply user preferences and quiet hours in the worker, not in the
/// publishing module"), which this Sprint's own build does not implement (the same
/// deferred-runtime split every sibling config aggregate in this framework draws for
/// itself) -- this aggregate only records the settings a future worker will read.
///
/// Raises no Domain Event: notification-framework.md's own Domain Events list names
/// none for preference changes, the same asymmetry <c>JobQueue.Register</c>'s own
/// remarks state for its own config aggregate.
///
/// <see cref="QuietHoursStart"/>/<see cref="QuietHoursEnd"/> are a time-of-day offset
/// from midnight, not a full timestamp -- <see cref="TimeSpan"/> is a long-proven EF
/// Core/Npgsql mapping (Postgres <c>interval</c>) already used safely elsewhere in this
/// codebase, unlike the unverified <c>TimeOnly</c> type this Sprint does not introduce
/// without first confirming its own mapping the way every other new pattern in this
/// codebase has been.
/// </summary>
public sealed class NotificationPreference : AggregateRoot<NotificationPreferenceId>
{
    public Guid TenantId { get; }

    public Guid UserId { get; }

    public string? PreferredLanguage { get; private set; }

    public IReadOnlyList<NotificationChannel> PreferredChannels { get; private set; }

    public TimeSpan? QuietHoursStart { get; private set; }

    public TimeSpan? QuietHoursEnd { get; private set; }

    public bool DigestMode { get; private set; }

    public bool OptedOut { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    private NotificationPreference(
        NotificationPreferenceId id,
        Guid tenantId,
        Guid userId,
        string? preferredLanguage,
        IReadOnlyList<NotificationChannel> preferredChannels,
        TimeSpan? quietHoursStart,
        TimeSpan? quietHoursEnd,
        bool digestMode,
        bool optedOut,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        UserId = userId;
        PreferredLanguage = preferredLanguage;
        PreferredChannels = preferredChannels;
        QuietHoursStart = quietHoursStart;
        QuietHoursEnd = quietHoursEnd;
        DigestMode = digestMode;
        OptedOut = optedOut;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>
    /// Registers a new preference row. Uniqueness of <paramref name="userId"/> within
    /// <paramref name="tenantId"/> is checked by the caller before this factory runs,
    /// the same split every other uniqueness-checked factory in this codebase
    /// establishes.
    /// </summary>
    public static Result<NotificationPreference> Register(
        Guid tenantId,
        Guid userId,
        string? preferredLanguage,
        IReadOnlyList<NotificationChannel> preferredChannels,
        TimeSpan? quietHoursStart,
        TimeSpan? quietHoursEnd,
        bool digestMode,
        bool optedOut,
        DateTimeOffset nowUtc)
    {
        Guard.AgainstDefault(tenantId, nameof(tenantId));
        Guard.AgainstDefault(userId, nameof(userId));

        var preference = new NotificationPreference(
            new NotificationPreferenceId(Guid.NewGuid()), tenantId, userId, preferredLanguage,
            preferredChannels ?? [], quietHoursStart, quietHoursEnd, digestMode, optedOut, nowUtc);

        return Result.Success(preference);
    }

    public Result Update(
        string? preferredLanguage,
        IReadOnlyList<NotificationChannel> preferredChannels,
        TimeSpan? quietHoursStart,
        TimeSpan? quietHoursEnd,
        bool digestMode,
        bool optedOut)
    {
        PreferredLanguage = preferredLanguage;
        PreferredChannels = preferredChannels ?? [];
        QuietHoursStart = quietHoursStart;
        QuietHoursEnd = quietHoursEnd;
        DigestMode = digestMode;
        OptedOut = optedOut;
        return Result.Success();
    }
}
