using Hris.SharedKernel;

namespace Hris.Foundation.Notification.Domain;

/// <summary>
/// Identity of the <see cref="NotificationPreference"/> Aggregate Root -- one user's own
/// notification settings within one tenant, per notification-framework.md's own User
/// Preferences section.
/// </summary>
public readonly record struct NotificationPreferenceId(Guid Value) : IStronglyTypedId;
