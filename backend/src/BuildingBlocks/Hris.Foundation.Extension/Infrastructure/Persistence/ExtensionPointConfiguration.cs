using Hris.Foundation.Extension.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.Extension.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="ExtensionPoint"/> Aggregate
/// Root, per coding-standards.md's Infrastructure Layer convention.
///
/// <see cref="ExtensionPoint.Key"/> is mapped through <c>HasConversion</c>, the same
/// choice this Sprint's own owned-type constructor-binding lesson (coding-standards.md's
/// "EF Core Persistence Pitfalls") makes proactively rather than reactively.
/// <see cref="ExtensionPoint.SupportedHookTypes"/> needed the identical
/// <c>HasConversion</c> treatment as <c>CountryConfiguration.WorkingDays</c>, but
/// without that property's own second constructor-binding defect: this aggregate's
/// constructor parameter and mapped property share the exact same declared type
/// (<c>IReadOnlyCollection&lt;HookType&gt;</c>), deliberately, so EF Core's
/// constructor-binding convention has an exact match to bind against and needs no
/// additive EF-only constructor -- confirmed by a real EF Core model build before this
/// class was committed, not assumed from the mapping shape alone.
/// </summary>
public sealed class ExtensionPointConfiguration : IEntityTypeConfiguration<ExtensionPoint>
{
    public void Configure(EntityTypeBuilder<ExtensionPoint> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(extensionPoint => extensionPoint.Id);

        builder.Property(extensionPoint => extensionPoint.Id)
            .HasConversion(new StronglyTypedIdValueConverter<ExtensionPointId>(value => new ExtensionPointId(value)))
            .ValueGeneratedNever();

        builder.Property(extensionPoint => extensionPoint.Key)
            .HasConversion(
                key => key.Value,
                value => ExtensionPointKey.Create(value).Value)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(extensionPoint => extensionPoint.Key).IsUnique();

        builder.Property(extensionPoint => extensionPoint.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(extensionPoint => extensionPoint.Description)
            .HasMaxLength(2000);

        builder.Property(extensionPoint => extensionPoint.ExtensionPointType).IsRequired();

        builder.Property(extensionPoint => extensionPoint.OwningModule)
            .HasMaxLength(200)
            .IsRequired();

        // SupportedHookTypes: a small, closed set (Before/After/Around) stored as a
        // comma-separated list of the underlying integer enum values -- a plain
        // delimited string, not JSON, matching CountryConfiguration.WorkingDays' own
        // identical precedent for the same reason (a closed BCL-shaped enum set gains
        // nothing from JSON's own nested-structure support).
        builder.Property(extensionPoint => extensionPoint.SupportedHookTypes)
            .HasConversion(
                types => string.Join(',', types.Select(t => (int)t)),
                value => string.IsNullOrEmpty(value)
                    ? (IReadOnlyCollection<HookType>)Array.Empty<HookType>()
                    : value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(part => (HookType)int.Parse(part, System.Globalization.CultureInfo.InvariantCulture))
                        .ToList())
            .IsRequired();

        builder.Property(extensionPoint => extensionPoint.Status).IsRequired();

        builder.Property(extensionPoint => extensionPoint.Version).IsRequired();
    }
}
