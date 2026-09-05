using Hris.SharedKernel;

namespace Hris.Foundation.Notification.Domain;

/// <summary>
/// Identity of a <see cref="NotificationTemplateVersion"/> child Entity, unique within
/// the context of its owning <see cref="NotificationTemplate"/> Aggregate -- the
/// identical shape <c>WorkflowDefinitionVersionId</c>'s own remarks already establish
/// for its sibling versioned child Entity.
/// </summary>
public readonly record struct NotificationTemplateVersionId(Guid Value) : IStronglyTypedId;
