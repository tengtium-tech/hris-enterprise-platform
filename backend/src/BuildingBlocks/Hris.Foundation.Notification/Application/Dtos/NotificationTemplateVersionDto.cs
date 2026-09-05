namespace Hris.Foundation.Notification.Application.Dtos;

public sealed record NotificationTemplateVersionDto(
    Guid NotificationTemplateVersionId,
    int VersionNumber,
    string Locale,
    string? Subject,
    string Body,
    IReadOnlyList<string> SupportedChannels,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? PublishedAtUtc);
