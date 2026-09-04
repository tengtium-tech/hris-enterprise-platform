using System.Text.Json;
using Hris.Foundation.StatutoryReferenceData.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.StatutoryReferenceData.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="StatutoryTableVersion"/>
/// Aggregate Root, per coding-standards.md's Infrastructure Layer convention.
///
/// <see cref="StatutoryTableVersion.Provenance"/> is mapped as a single JSON column via
/// <c>HasConversion</c>, not <c>OwnsOne</c> -- the identical "a converter is simpler than
/// an owned/Complex Type when there is only one underlying column, and no independent
/// per-row query need of its own component parts" choice
/// <c>SearchIndexDefinitionConfiguration</c>'s own remarks already make for
/// <c>SearchIndexDefinition.Fields</c>, and the same choice that sidesteps the Complex
/// Type constructor-binding limitation <c>NumberSeries</c>' own history in this codebase
/// first surfaced. A <c>ValueComparer</c> IS registered here, unlike that Search
/// precedent: <see cref="StatutoryTableVersion.RecordSignoff"/> replaces
/// <see cref="StatutoryTableVersion.Provenance"/> with a structurally-different-but-
/// reference-different <c>record</c> via <c>with</c>, and EF Core's default reference
/// equality change detection already sees that (a new object reference), so the
/// <c>ValueComparer</c> here is precautionary correctness for records specifically
/// (whose own <c>Equals</c> is value-based, which could otherwise confuse a snapshot
/// comparison) rather than a functional requirement change-tracking needs.
///
/// <see cref="StatutoryTableVersion.ScheduleData"/> is mapped to a Postgres
/// <c>jsonb</c> column directly as a raw string -- opaque to this framework, per that
/// property's own remarks; unlike <see cref="StatutoryTableVersion.Provenance"/> it
/// needs no <c>HasConversion</c> since it already is the string EF Core persists.
/// </summary>
public sealed class StatutoryTableVersionConfiguration : IEntityTypeConfiguration<StatutoryTableVersion>
{
    public void Configure(EntityTypeBuilder<StatutoryTableVersion> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(version => version.Id);

        builder.Property(version => version.Id)
            .HasConversion(new StronglyTypedIdValueConverter<StatutoryTableVersionId>(value => new StatutoryTableVersionId(value)))
            .ValueGeneratedNever();

        builder.Property(version => version.StatutoryProgramId)
            .HasConversion(new StronglyTypedIdValueConverter<StatutoryProgramId>(value => new StatutoryProgramId(value)))
            .IsRequired();

        builder.HasIndex(version => new { version.StatutoryProgramId, version.EffectiveFromUtc });

        builder.Property(version => version.VersionLabel)
            .HasConversion(
                label => label.Value,
                value => StatutoryTableVersionLabel.Create(value).Value)
            .HasMaxLength(7)
            .IsRequired();

        builder.HasIndex(version => new { version.StatutoryProgramId, version.VersionLabel }).IsUnique();

        builder.Property(version => version.EffectiveFromUtc).IsRequired();

        builder.Property(version => version.EffectiveToUtc);

        builder.Property(version => version.Provenance)
            .HasConversion(
                provenance => JsonSerializer.Serialize(provenance, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<StatutoryTableProvenance>(json, (JsonSerializerOptions?)null)!)
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<StatutoryTableProvenance>(
                (left, right) => left! == right,
                provenance => provenance.GetHashCode()));

        builder.Property(version => version.ScheduleData)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(version => version.PublishedAtUtc).IsRequired();
    }
}
