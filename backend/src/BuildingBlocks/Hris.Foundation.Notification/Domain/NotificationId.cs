using Hris.SharedKernel;

namespace Hris.Foundation.Notification.Domain;

/// <summary>
/// Identity of the <see cref="Notification"/> Aggregate Root -- one notification's own
/// journey from request through delivery on one channel, per notification-framework.md's
/// own Notification Lifecycle and Delivery Tracking sections.
/// </summary>
public readonly record struct NotificationId(Guid Value) : IStronglyTypedId;
