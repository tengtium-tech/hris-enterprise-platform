namespace Hris.Foundation.Notification.Application.Dtos;

public sealed record NotificationPreferenceDto(
    Guid NotificationPreferenceId,
    Guid TenantId,
    Guid UserId,
    string? PreferredLanguage,
    IReadOnlyList<string> PreferredChannels,
    TimeSpan? QuietHoursStart,
    TimeSpan? QuietHoursEnd,
    bool DigestMode,
    bool OptedOut,
    DateTimeOffset CreatedAtUtc);
