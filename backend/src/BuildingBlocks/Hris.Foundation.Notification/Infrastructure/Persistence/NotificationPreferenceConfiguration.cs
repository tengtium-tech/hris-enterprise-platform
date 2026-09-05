using System.Text.Json;
using Hris.Foundation.Notification.Domain;
using Hris.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.Notification.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="NotificationPreference"/>
/// Aggregate Root, per coding-standards.md's Infrastructure Layer convention.
/// <see cref="NotificationPreference.PreferredChannels"/> is mapped as a single JSON
/// column via <c>HasConversion</c>, the same choice
/// <c>NotificationTemplateConfiguration</c>'s own remarks make for
/// <c>NotificationTemplateVersion.SupportedChannels</c>.
/// </summary>
public sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(preference => preference.Id);

        builder.Property(preference => preference.Id)
            .HasConversion(new StronglyTypedIdValueConverter<NotificationPreferenceId>(value => new NotificationPreferenceId(value)))
            .ValueGeneratedNever();

        builder.Property(preference => preference.TenantId).IsRequired();

        builder.Property(preference => preference.UserId).IsRequired();

        builder.HasIndex(preference => new { preference.TenantId, preference.UserId }).IsUnique();

        builder.Property(preference => preference.PreferredLanguage).HasMaxLength(20);

        builder.Property(preference => preference.PreferredChannels)
            .HasConversion(
                channels => JsonSerializer.Serialize(channels, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<NotificationChannel>>(json, (JsonSerializerOptions?)null)
                    ?? new List<NotificationChannel>())
            .IsRequired();

        builder.Property(preference => preference.QuietHoursStart);
        builder.Property(preference => preference.QuietHoursEnd);

        builder.Property(preference => preference.DigestMode).IsRequired();
        builder.Property(preference => preference.OptedOut).IsRequired();

        builder.Property(preference => preference.CreatedAtUtc).IsRequired();
    }
}
