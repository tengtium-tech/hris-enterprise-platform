using Hris.Foundation.Numbering.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.Numbering.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="IssuedNumber"/> Aggregate Root,
/// per coding-standards.md's Infrastructure Layer convention.
/// </summary>
public sealed class IssuedNumberConfiguration : IEntityTypeConfiguration<IssuedNumber>
{
    public void Configure(EntityTypeBuilder<IssuedNumber> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(number => number.Id);

        builder.Property(number => number.Id)
            .HasConversion(new StronglyTypedIdValueConverter<IssuedNumberId>(value => new IssuedNumberId(value)))
            .ValueGeneratedNever();

        builder.Property(number => number.NumberSeriesId)
            .HasConversion(new StronglyTypedIdValueConverter<NumberSeriesId>(value => new NumberSeriesId(value)))
            .IsRequired();

        builder.HasIndex(number => number.NumberSeriesId);

        builder.Property(number => number.SequenceValue);

        builder.Property(number => number.FormattedNumber)
            .HasConversion(
                formatted => formatted == null ? null : formatted.Value,
                value => value == null ? null : FormattedNumber.Create(value).Value)
            .HasMaxLength(100);

        builder.Property(number => number.Status).IsRequired();

        builder.Property(number => number.AssignedToType).HasMaxLength(200);

        builder.Property(number => number.AssignedToReferenceId).HasMaxLength(200);

        builder.Property(number => number.RequestedAtUtc).IsRequired();

        builder.Property(number => number.IssuedAtUtc);
    }
}
