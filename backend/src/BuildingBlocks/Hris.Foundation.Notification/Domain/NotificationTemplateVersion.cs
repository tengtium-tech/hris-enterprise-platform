using Hris.SharedKernel;

namespace Hris.Foundation.Notification.Domain;

/// <summary>
/// One version of a <see cref="NotificationTemplate"/>'s own rendered content, per
/// notification-framework.md's own Message Templates section ("Subject, Title, Body,
/// Attachments, Variables, Conditional Content, Branding, Localization... Templates
/// should be versioned"). A child Entity of the <see cref="NotificationTemplate"/>
/// Aggregate, never an Aggregate Root of its own -- the identical shape and reasoning
/// <c>WorkflowDefinitionVersion</c>'s own remarks already establish: a template's own
/// version count is small and bounded to one aggregate's own consistency boundary,
/// unlike the genuinely population-scale <see cref="Notification"/> aggregate this
/// Sprint's own sibling document also builds. Its constructor and every transition
/// method are <c>internal</c>, reachable only through
/// <see cref="NotificationTemplate"/>'s own methods.
///
/// <see cref="Body"/> is stored as opaque text carrying its own <c>{{Variable}}</c>-style
/// placeholders (Template Variables section: "Employee Name, Manager Name, Company,"
/// and so on) -- this Sprint's own build records the template, it does not build a
/// variable-substitution rendering engine, the identical "records the configuration,
/// does not build the runtime that walks it" split every Sprint 4/5 config-plus-
/// occurrence framework already draws for its own out-of-scope runtime concern.
/// </summary>
public sealed class NotificationTemplateVersion : Entity<NotificationTemplateVersionId>
{
    public int VersionNumber { get; }

    public string Locale { get; }

    public string? Subject { get; }

    public string Body { get; }

    public IReadOnlyList<NotificationChannel> SupportedChannels { get; }

    public NotificationTemplateVersionStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset? PublishedAtUtc { get; private set; }

    internal NotificationTemplateVersion(
        NotificationTemplateVersionId id,
        int versionNumber,
        string locale,
        string? subject,
        string body,
        IReadOnlyList<NotificationChannel> supportedChannels,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        VersionNumber = versionNumber;
        Locale = locale;
        Subject = subject;
        Body = body;
        SupportedChannels = supportedChannels;
        Status = NotificationTemplateVersionStatus.Draft;
        CreatedAtUtc = createdAtUtc;
    }

    internal Result Publish(DateTimeOffset nowUtc)
    {
        if (Status != NotificationTemplateVersionStatus.Draft)
        {
            return Result.Failure(NotificationErrors.InvalidVersionLifecycleTransition);
        }

        Status = NotificationTemplateVersionStatus.Published;
        PublishedAtUtc = nowUtc;
        return Result.Success();
    }

    internal Result Deprecate()
    {
        if (Status != NotificationTemplateVersionStatus.Published)
        {
            return Result.Failure(NotificationErrors.InvalidVersionLifecycleTransition);
        }

        Status = NotificationTemplateVersionStatus.Deprecated;
        return Result.Success();
    }
}
