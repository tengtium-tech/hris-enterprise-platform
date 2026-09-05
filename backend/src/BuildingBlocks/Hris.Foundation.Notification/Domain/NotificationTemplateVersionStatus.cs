namespace Hris.Foundation.Notification.Domain;

/// <summary>
/// notification-framework.md's own Message Templates section: "Templates should be
/// versioned." The identical Draft/Published/Deprecated shape
/// <c>WorkflowDefinitionVersionStatus</c>'s own remarks already establish for its
/// sibling versioned child Entity, for the identical reason: a template's own version
/// count is small and bounded to one aggregate's own consistency boundary, never
/// population-scale.
/// </summary>
public enum NotificationTemplateVersionStatus
{
    Draft = 0,
    Published = 1,
    Deprecated = 2,
}
