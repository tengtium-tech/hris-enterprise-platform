using Hris.SharedKernel;

namespace Hris.Foundation.Notification.Domain;

/// <summary>
/// Identity of the <see cref="NotificationTemplate"/> Aggregate Root -- one reusable
/// message template, per notification-framework.md's own Message Templates section.
/// </summary>
public readonly record struct NotificationTemplateId(Guid Value) : IStronglyTypedId;
