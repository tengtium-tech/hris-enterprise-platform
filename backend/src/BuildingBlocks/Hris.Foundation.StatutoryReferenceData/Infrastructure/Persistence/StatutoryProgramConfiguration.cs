using Hris.Foundation.StatutoryReferenceData.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.StatutoryReferenceData.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="StatutoryProgram"/> Aggregate
/// Root, per coding-standards.md's Infrastructure Layer convention. No owned type or
/// Complex Type anywhere in this shape, and every constructor parameter on
/// <see cref="StatutoryProgram"/> already shares its name with the property it sets
/// (see that class's own remarks), so this configuration needs no second,
/// EF-materialization-only constructor -- confirmed by a real model build, not assumed.
/// </summary>
public sealed class StatutoryProgramConfiguration : IEntityTypeConfiguration<StatutoryProgram>
{
    public void Configure(EntityTypeBuilder<StatutoryProgram> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(program => program.Id);

        builder.Property(program => program.Id)
            .HasConversion(new StronglyTypedIdValueConverter<StatutoryProgramId>(value => new StatutoryProgramId(value)))
            .ValueGeneratedNever();

        builder.Property(program => program.Code)
            .HasConversion(
                code => code.Value,
                value => StatutoryProgramCode.Create(value).Value)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(program => program.Country)
            .HasConversion(
                country => country.Value,
                value => StatutoryCountryCode.Create(value).Value)
            .HasMaxLength(2)
            .IsRequired();

        builder.HasIndex(program => new { program.Country, program.Code }).IsUnique();

        builder.Property(program => program.DisplayName).HasMaxLength(200).IsRequired();

        builder.Property(program => program.RegisteredAtUtc).IsRequired();
    }
}
