using System.Text.Json;
using Hris.Foundation.Audit.Domain;
using Hris.Foundation.Identity.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.Audit.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for <see cref="AuditRecord"/>, per
/// coding-standards.md's Infrastructure Layer convention. Not an
/// <c>AggregateRoot</c> in this codebase's own usual sense (see that type's own
/// remarks), but still the one top-level, directly persisted entity this framework
/// owns -- dbcontext-design.md's "Only Aggregate Roots are persisted directly" names
/// the usual DDD shape that satisfies, not a hard requirement that every mapped root
/// type inherit that exact base class; a plain, immutable <see cref="Entity{TId}"/>
/// with no child collections and no business behavior is the correct shape for a
/// write-once compliance fact, per that type's own extensive remarks.
///
/// Discovered automatically by <see cref="HrisDbContext.OnModelCreating"/> via
/// <see cref="PersistenceAssemblyRegistry"/>.
/// </summary>
public sealed class AuditRecordConfiguration : IEntityTypeConfiguration<AuditRecord>
{
    public void Configure(EntityTypeBuilder<AuditRecord> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(record => record.Id);

        builder.Property(record => record.Id)
            .HasConversion(new StronglyTypedIdValueConverter<AuditRecordId>(value => new AuditRecordId(value)))
            .ValueGeneratedNever();

        builder.Property(record => record.TimestampUtc).IsRequired();

        // audit-framework.md's own Audit Search section names Date Range, User,
        // Business Entity, and Correlation Identifier as filter dimensions --
        // each of the four indexes below backs one of those directly, the same
        // NFR-PF-001-driven reasoning RoleAssignmentConfiguration's own remarks state
        // for its own indexes.
        builder.HasIndex(record => record.TimestampUtc);

        builder.Property(record => record.ActorId)
            .HasConversion(
                actorId => actorId.HasValue ? actorId.Value.Value : (Guid?)null,
                value => value.HasValue ? new UserAccountId(value.Value) : (UserAccountId?)null);

        builder.HasIndex(record => record.ActorId);

        builder.Property(record => record.Category).IsRequired();

        builder.Property(record => record.Action).HasMaxLength(200).IsRequired();

        builder.Property(record => record.BusinessEntity).HasMaxLength(200).IsRequired();

        builder.HasIndex(record => record.BusinessEntity);

        builder.Property(record => record.EntityIdentifier).HasMaxLength(200).IsRequired();

        // Snapshot payloads -- unbounded, per audit-data.md's own persistence model
        // rather than an arbitrary length cap here.
        builder.Property(record => record.PreviousValue);
        builder.Property(record => record.NewValue);

        builder.Property(record => record.SourceSystem).HasMaxLength(200).IsRequired();
        builder.Property(record => record.ClientApplication).HasMaxLength(200);
        builder.Property(record => record.IpAddress).HasMaxLength(45); // IPv6 textual max length.
        builder.Property(record => record.DeviceInformation).HasMaxLength(500);

        builder.Property(record => record.CorrelationId)
            .HasConversion(
                correlationId => correlationId != null ? correlationId.Value : (Guid?)null,
                value => value.HasValue ? CorrelationId.Create(value.Value).Value : null);

        builder.HasIndex(record => record.CorrelationId);

        builder.Property(record => record.Outcome).IsRequired();

        // Metadata: a dictionary, serialized to JSON -- the identical choice
        // OutboxEntryConfiguration's own remarks make for EventEnvelope.Metadata, for
        // the same reason (EF Core cannot map a dictionary as a relational column
        // directly).
        builder.Property(record => record.Metadata)
            .HasConversion(
                metadata => JsonSerializer.Serialize(metadata, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<Dictionary<string, string>>(json, (JsonSerializerOptions?)null)
                    ?? new Dictionary<string, string>())
            .IsRequired();
    }
}
