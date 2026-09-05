namespace Hris.Foundation.Notification.Application.Dtos;

public sealed record NotificationTemplateDto(
    Guid NotificationTemplateId,
    Guid TenantId,
    string TemplateKey,
    string NotificationType,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<NotificationTemplateVersionDto> Versions);
