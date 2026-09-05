using Hris.Foundation.Notification.Application.Dtos;
using Hris.Foundation.Notification.Domain;

namespace Hris.Foundation.Notification.Application.Mapping;

/// <summary>
/// Domain-to-DTO mapping, kept as a plain static class rather than a library such as
/// AutoMapper, per mapping.md's own stated preference for explicit mapping code -- the
/// identical choice every other Sprint 3/4/5 framework's own mapper already
/// establishes.
/// </summary>
internal static class NotificationMapper
{
    public static NotificationTemplateVersionDto ToDto(NotificationTemplateVersion version) => new(
        version.Id.Value,
        version.VersionNumber,
        version.Locale,
        version.Subject,
        version.Body,
        version.SupportedChannels.Select(c => c.ToString()).ToList(),
        version.Status.ToString(),
        version.CreatedAtUtc,
        version.PublishedAtUtc);

    public static NotificationTemplateDto ToDto(NotificationTemplate template) => new(
        template.Id.Value,
        template.TenantId,
        template.TemplateKey,
        template.NotificationType.ToString(),
        template.CreatedAtUtc,
        template.Versions.Select(ToDto).ToList());

    public static NotificationDto ToDto(Domain.Notification notification) => new(
        notification.Id.Value,
        notification.TenantId,
        notification.RecipientUserId,
        notification.NotificationType.ToString(),
        notification.Channel.ToString(),
        notification.TemplateKey,
        notification.Subject,
        notification.Body,
        notification.Status.ToString(),
        notification.RequestedAtUtc,
        notification.ScheduledForUtc,
        notification.SentAtUtc,
        notification.DeliveredAtUtc,
        notification.ReadAtUtc,
        notification.AcknowledgedAtUtc,
        notification.FailureReason,
        notification.RetryCount,
        notification.CancellationReason);

    public static NotificationPreferenceDto ToDto(NotificationPreference preference) => new(
        preference.Id.Value,
        preference.TenantId,
        preference.UserId,
        preference.PreferredLanguage,
        preference.PreferredChannels.Select(c => c.ToString()).ToList(),
        preference.QuietHoursStart,
        preference.QuietHoursEnd,
        preference.DigestMode,
        preference.OptedOut,
        preference.CreatedAtUtc);
}
