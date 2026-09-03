using Hris.Foundation.Numbering.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.Numbering.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="NumberSeries"/> Aggregate Root,
/// per coding-standards.md's Infrastructure Layer convention. No owned type anywhere in
/// this shape -- every property is a scalar or a single-column <c>HasConversion</c>
/// Value Object, so this configuration needs none of <c>StoredFileConfiguration</c>'s
/// own owned-table handling.
/// </summary>
public sealed class NumberSeriesConfiguration : IEntityTypeConfiguration<NumberSeries>
{
    public void Configure(EntityTypeBuilder<NumberSeries> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(series => series.Id);

        builder.Property(series => series.Id)
            .HasConversion(new StronglyTypedIdValueConverter<NumberSeriesId>(value => new NumberSeriesId(value)))
            .ValueGeneratedNever();

        builder.Property(series => series.Key)
            .HasConversion(
                key => key.Value,
                value => SeriesKey.Create(value).Value)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(series => series.Key).IsUnique();

        builder.Property(series => series.Prefix)
            .HasConversion(
                prefix => prefix.Value,
                value => NumberPrefix.Create(value).Value)
            .HasMaxLength(10)
            .IsRequired();

        // NumberFormat: an EF Core 9 Complex Type, not OwnsOne and not a single-column
        // HasConversion -- each component (running-number length, year/month
        // inclusion, separator) has its own independent meaning a query might
        // reasonably filter or report on, unlike SupportedHookTypes' own closed-set
        // collection, which HasConversion suits better; and unlike PendingVersion/
        // Versions in StoredFileConfiguration, NumberFormat has no identity of its own
        // and never needs a second, independent instance sharing this same CLR type,
        // so it needs neither its own key nor its own table -- a Complex Type maps its
        // four fields as four ordinary scalar columns on this same table, the first
        // use of this EF Core feature in this codebase.
        builder.ComplexProperty(series => series.Format, format =>
        {
            format.Property(f => f.RunningNumberLength).HasColumnName("running_number_length").IsRequired();
            format.Property(f => f.IncludeYear).HasColumnName("include_year").IsRequired();
            format.Property(f => f.IncludeMonth).HasColumnName("include_month").IsRequired();
            format.Property(f => f.Separator).HasColumnName("separator").HasMaxLength(3).IsRequired();
        });

        builder.Property(series => series.ResetPolicy).IsRequired();

        builder.Property(series => series.CurrentSequenceValue).IsRequired();

        builder.Property(series => series.LastResetAtUtc);
    }
}
