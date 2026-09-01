using System.Text.Json;
using Hris.Foundation.Events.Domain;
using Hris.Foundation.Identity.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.Events.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="OutboxEntry"/> Aggregate Root
/// and its owned <see cref="EventEnvelope"/>, per coding-standards.md's Infrastructure
/// Layer convention and aggregate-persistence.md -- the identical shape
/// <c>ConfigurationSettingConfiguration</c>/<c>UserAccountConfiguration</c> already
/// establish. <see cref="EventEnvelope"/> is mapped as a single Owned Type (EF Core's
/// <c>OwnsOne</c>) in the same table, the same choice <c>ConfigurationSettingConfiguration</c>
/// makes for <c>ConfigurationScope</c> -- there is exactly one envelope per entry.
///
/// Discovered automatically by <see cref="HrisDbContext.OnModelCreating"/> via
/// <see cref="PersistenceAssemblyRegistry"/>.
/// </summary>
public sealed class OutboxEntryConfiguration : IEntityTypeConfiguration<OutboxEntry>
{
    public void Configure(EntityTypeBuilder<OutboxEntry> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Id)
            .HasConversion(new StronglyTypedIdValueConverter<OutboxEntryId>(value => new OutboxEntryId(value)))
            .ValueGeneratedNever();

        builder.Property(entry => entry.Status).IsRequired();

        // outbox-pattern.md's own Performance section: "The Outbox table should
        // support... Indexes on processing status" -- exactly the column
        // GetPendingBatchAsync's own WHERE clause filters on.
        builder.HasIndex(entry => entry.Status);

        builder.Property(entry => entry.CreatedAtUtc).IsRequired();
        builder.Property(entry => entry.DispatchedAtUtc);
        builder.Property(entry => entry.AttemptCount).IsRequired();
        builder.Property(entry => entry.LastAttemptAtUtc);
        builder.Property(entry => entry.LastFailureReason).HasMaxLength(2000);

        builder.OwnsOne(entry => entry.Envelope, envelope =>
        {
            envelope.Property(e => e.EventId).HasColumnName("EventId").IsRequired();
            envelope.Property(e => e.EventType).HasColumnName("EventType").HasMaxLength(200).IsRequired();
            envelope.Property(e => e.EventVersion).HasColumnName("EventVersion").IsRequired();
            envelope.Property(e => e.OccurredOnUtc).HasColumnName("OccurredOnUtc").IsRequired();
            envelope.Property(e => e.SourceModule).HasColumnName("SourceModule").HasMaxLength(200).IsRequired();
            envelope.Property(e => e.Category).HasColumnName("Category").IsRequired();

            // CorrelationId: the same single-property Value Object -> Value Converter
            // choice ConfigurationSettingConfiguration makes for ConfigurationKey.
            envelope.Property(e => e.CorrelationId)
                .HasConversion(
                    correlationId => correlationId.Value,
                    value => CorrelationId.Create(value).Value)
                .HasColumnName("CorrelationId")
                .IsRequired();

            envelope.Property(e => e.TenantId).HasColumnName("TenantId");
            envelope.Property(e => e.CompanyId).HasColumnName("CompanyId");

            // Actor: a nullable Strongly Typed Id -- StronglyTypedIdValueConverter<TId>
            // targets the non-nullable shape only, so this one column needs its own
            // inline nullable conversion rather than that shared converter.
            envelope.Property(e => e.Actor)
                .HasConversion(
                    actor => actor.HasValue ? actor.Value.Value : (Guid?)null,
                    value => value.HasValue ? new UserAccountId(value.Value) : (UserAccountId?)null)
                .HasColumnName("ActorUserAccountId");

            // Payload: already-serialized text (see EventEnvelope's own remarks) --
            // stored as-is, no further conversion needed.
            envelope.Property(e => e.Payload).HasColumnName("Payload").IsRequired();

            // Metadata: a dictionary, which EF Core cannot map as a relational column
            // directly -- serialized to a JSON string, the same "portable format such
            // as JSON" outbox-pattern.md's own Outbox Table section calls for the
            // payload itself to use.
            envelope.Property(e => e.Metadata)
                .HasConversion(
                    metadata => JsonSerializer.Serialize(metadata, (JsonSerializerOptions?)null),
                    json => JsonSerializer.Deserialize<Dictionary<string, string>>(json, (JsonSerializerOptions?)null)
                        ?? new Dictionary<string, string>())
                .HasColumnName("Metadata")
                .IsRequired();
        });

        builder.Navigation(entry => entry.Envelope).IsRequired();
    }
}
