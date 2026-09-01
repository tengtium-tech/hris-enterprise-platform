using System.Text.Json;
using Hris.Foundation.Localization.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.Localization.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="TranslationEntry"/> Aggregate
/// Root, per coding-standards.md's Infrastructure Layer convention.
///
/// <see cref="Domain.TranslationEntry"/>'s own private constructor takes only
/// <c>id</c> and <c>key</c> -- both scalar/converted, both bindable as-is -- so,
/// like <see cref="CountryConfiguration"/>, this aggregate needs no second,
/// EF-Core-only constructor: <c>TranslationsByLocale</c> and <c>VersionNumber</c>
/// are populated post-construction (through <c>SetTranslation</c> in application
/// code, through this configuration's own field/property access for EF Core), never
/// through the constructor itself.
/// </summary>
public sealed class TranslationEntryConfiguration : IEntityTypeConfiguration<TranslationEntry>
{
    public void Configure(EntityTypeBuilder<TranslationEntry> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Id)
            .HasConversion(new StronglyTypedIdValueConverter<TranslationEntryId>(value => new TranslationEntryId(value)))
            .ValueGeneratedNever();

        // Key: the natural key ITranslationEntryRepository actually looks up by
        // (that interface's own remarks) -- one entry per key, per this aggregate's
        // own Create uniqueness check.
        builder.Property(entry => entry.Key)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(entry => entry.Key).IsUnique();

        builder.Property(entry => entry.VersionNumber).IsRequired();

        // TranslationsByLocale: a Dictionary<string,string>, serialized to JSON --
        // the identical choice OutboxEntryConfiguration's own remarks make for
        // EventEnvelope.Metadata. Unlike Metadata (a plain get-only auto-property),
        // this property is an expression body reading the backing field directly
        // (`=> _translationsByLocale`), with no usable setter -- explicit
        // PropertyAccessMode.Field on the property builder itself below (not
        // Navigation(), which is for owned-type/relationship navigations, and this
        // is a HasConversion-mapped scalar property, not one), the same defensive
        // choice ConfigurationSettingConfiguration's own remarks make for Versions.
        builder.Property(entry => entry.TranslationsByLocale)
            .HasConversion(
                translations => JsonSerializer.Serialize(translations, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<Dictionary<string, string>>(json, (JsonSerializerOptions?)null)
                    ?? new Dictionary<string, string>())
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired();
    }
}
