using Hris.Foundation.Notification.Domain;
using Hris.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.Notification.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="Notification"/> Aggregate Root,
/// per coding-standards.md's Infrastructure Layer convention. No owned type or Complex
/// Type anywhere in this shape, and every constructor parameter already shares its name
/// with the property it sets, so this configuration needs no second,
/// EF-materialization-only constructor -- confirmed by a real model build, not assumed.
/// </summary>
public sealed class NotificationConfiguration : IEntityTypeConfiguration<Domain.Notification>
{
    public void Configure(EntityTypeBuilder<Domain.Notification> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(notification => notification.Id);

        builder.Property(notification => notification.Id)
            .HasConversion(new StronglyTypedIdValueConverter<NotificationId>(value => new NotificationId(value)))
            .ValueGeneratedNever();

        builder.Property(notification => notification.TenantId).IsRequired();

        builder.Property(notification => notification.RecipientUserId).IsRequired();

        builder.HasIndex(notification => new { notification.RecipientUserId, notification.TenantId, notification.Channel, notification.Status });

        builder.Property(notification => notification.NotificationType).IsRequired();

        builder.Property(notification => notification.Channel).IsRequired();

        builder.Property(notification => notification.TemplateKey).HasMaxLength(200);

        builder.Property(notification => notification.Subject).HasMaxLength(500);

        builder.Property(notification => notification.Body).IsRequired();

        builder.Property(notification => notification.Status).IsRequired();

        builder.Property(notification => notification.RequestedAtUtc).IsRequired();

        builder.Property(notification => notification.ScheduledForUtc);
        builder.Property(notification => notification.SentAtUtc);
        builder.Property(notification => notification.DeliveredAtUtc);
        builder.Property(notification => notification.ReadAtUtc);
        builder.Property(notification => notification.AcknowledgedAtUtc);

        builder.Property(notification => notification.FailureReason).HasMaxLength(2000);

        builder.Property(notification => notification.RetryCount).IsRequired();

        builder.Property(notification => notification.CancellationReason).HasMaxLength(2000);
    }
}
