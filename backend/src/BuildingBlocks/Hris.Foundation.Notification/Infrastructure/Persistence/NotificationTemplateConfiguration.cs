using System.Text.Json;
using Hris.Foundation.Notification.Domain;
using Hris.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.Notification.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="NotificationTemplate"/>
/// Aggregate Root and its <see cref="NotificationTemplateVersion"/> child Entity, per
/// coding-standards.md's Infrastructure Layer convention. <c>OwnsMany</c> with
/// <c>PropertyAccessMode.Field</c> on <see cref="NotificationTemplate.Versions"/> is the
/// identical shape <c>WorkflowDefinitionConfiguration</c>'s own remarks already
/// establish for its own sibling versioned child Entity.
///
/// <see cref="NotificationTemplateVersion.SupportedChannels"/> is mapped as a single
/// JSON column via <c>HasConversion</c> inside the <c>OwnsMany</c> builder -- the same
/// converter-simpler-than-owned-type choice
/// <c>WorkflowDefinitionConfiguration</c>'s own remarks already make for
/// <c>WorkflowDefinitionVersion.Steps</c>, confirmed by a real model build.
/// </summary>
public sealed class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(template => template.Id);

        builder.Property(template => template.Id)
            .HasConversion(new StronglyTypedIdValueConverter<NotificationTemplateId>(value => new NotificationTemplateId(value)))
            .ValueGeneratedNever();

        builder.Property(template => template.TenantId).IsRequired();

        builder.Property(template => template.TemplateKey).HasMaxLength(200).IsRequired();

        builder.HasIndex(template => new { template.TenantId, template.TemplateKey }).IsUnique();

        builder.Property(template => template.NotificationType).IsRequired();

        builder.Property(template => template.CreatedAtUtc).IsRequired();

        builder.OwnsMany(template => template.Versions, version =>
        {
            version.WithOwner().HasForeignKey("NotificationTemplateId");

            version.HasKey(v => v.Id);

            version.Property(v => v.Id)
                .HasConversion(new StronglyTypedIdValueConverter<NotificationTemplateVersionId>(value => new NotificationTemplateVersionId(value)))
                .ValueGeneratedNever();

            version.Property(v => v.VersionNumber).IsRequired();
            version.Property(v => v.Locale).HasMaxLength(20).IsRequired();
            version.Property(v => v.Subject).HasMaxLength(500);
            version.Property(v => v.Body).IsRequired();

            version.Property(v => v.SupportedChannels)
                .HasConversion(
                    channels => JsonSerializer.Serialize(channels, (JsonSerializerOptions?)null),
                    json => JsonSerializer.Deserialize<List<NotificationChannel>>(json, (JsonSerializerOptions?)null)
                        ?? new List<NotificationChannel>())
                .IsRequired();

            version.Property(v => v.Status).IsRequired();
            version.Property(v => v.CreatedAtUtc).IsRequired();
            version.Property(v => v.PublishedAtUtc);
        });

        builder.Navigation(template => template.Versions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
