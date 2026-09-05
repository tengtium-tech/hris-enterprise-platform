using Hris.SharedKernel;

namespace Hris.Foundation.Notification.Domain;

/// <summary>
/// This bounded context's own reusable error catalog, per error-pattern.md's "Error
/// Catalog" section.
/// </summary>
public static class NotificationErrors
{
    public static readonly Error TemplateKeyRequired = new(
        "Notification.TemplateKeyRequired",
        "A template key is required.",
        ErrorCategory.Validation);

    public static readonly Error SupportedChannelsRequired = new(
        "Notification.SupportedChannelsRequired",
        "At least one supported delivery channel is required.",
        ErrorCategory.Validation);

    public static readonly Error BodyRequired = new(
        "Notification.BodyRequired",
        "A template body is required.",
        ErrorCategory.Validation);

    public static readonly Error LocaleRequired = new(
        "Notification.LocaleRequired",
        "A locale is required.",
        ErrorCategory.Validation);

    public static readonly Error TemplateNotFound = new(
        "Notification.TemplateNotFound",
        "No notification template exists for the given identifier.",
        ErrorCategory.NotFound);

    public static readonly Error DuplicateTemplateKey = new(
        "Notification.DuplicateTemplateKey",
        "A notification template with this key already exists for this tenant.",
        ErrorCategory.Conflict);

    public static readonly Error DraftAlreadyExists = new(
        "Notification.DraftAlreadyExists",
        "This notification template already has an unpublished draft version.",
        ErrorCategory.Conflict);

    public static readonly Error VersionNotFound = new(
        "Notification.VersionNotFound",
        "No version exists for the given version number on this notification template.",
        ErrorCategory.NotFound);

    public static readonly Error InvalidVersionLifecycleTransition = new(
        "Notification.InvalidVersionLifecycleTransition",
        "This transition is not valid from the template version's current status.",
        ErrorCategory.Domain);

    public static readonly Error NotificationNotFound = new(
        "Notification.NotificationNotFound",
        "No notification exists for the given identifier.",
        ErrorCategory.NotFound);

    public static readonly Error InvalidNotificationLifecycleTransition = new(
        "Notification.InvalidNotificationLifecycleTransition",
        "This transition is not valid from the notification's current status.",
        ErrorCategory.Domain);

    public static readonly Error FailureReasonRequired = new(
        "Notification.FailureReasonRequired",
        "A reason is required to fail a notification.",
        ErrorCategory.Validation);

    public static readonly Error CancellationReasonRequired = new(
        "Notification.CancellationReasonRequired",
        "A reason is required to cancel a notification.",
        ErrorCategory.Validation);

    public static readonly Error NotAuthorizedForThisNotification = new(
        "Notification.NotAuthorizedForThisNotification",
        "A notification may only be marked read by its own recipient.",
        ErrorCategory.Authorization);

    public static readonly Error PreferenceNotFound = new(
        "Notification.PreferenceNotFound",
        "No notification preference exists for the given user.",
        ErrorCategory.NotFound);

    public static readonly Error PreferenceAlreadyExists = new(
        "Notification.PreferenceAlreadyExists",
        "A notification preference already exists for this user in this tenant.",
        ErrorCategory.Conflict);
}
