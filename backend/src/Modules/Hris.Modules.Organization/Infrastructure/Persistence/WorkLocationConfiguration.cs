using Hris.Infrastructure.Persistence;
using Hris.Modules.Organization.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Organization.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="WorkLocation"/> Aggregate
/// Root. <see cref="Address"/> is mapped as an owned type (table-split into this
/// same table, EF Core's own default for a required single <c>OwnsOne</c> with no
/// competing owned navigation) since it carries several independently meaningful
/// columns; <see cref="WorkLocationTimeZone"/> is a single-column Value Object and
/// uses a plain <c>HasConversion</c> instead, the same "prefer a converter over a
/// nested owned type for a Value Object with no independent query need of its own
/// component parts" choice <c>StoredFileConfiguration</c> already states for
/// <c>Checksum</c>.
/// </summary>
public sealed class WorkLocationConfiguration : IEntityTypeConfiguration<WorkLocation>
{
    public void Configure(EntityTypeBuilder<WorkLocation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Id)
            .HasConversion(new StronglyTypedIdValueConverter<WorkLocationId>(value => new WorkLocationId(value)))
            .ValueGeneratedNever();

        builder.Property(w => w.TenantId).IsRequired();
        builder.HasIndex(w => w.TenantId);

        builder.Property(w => w.Code)
            .HasConversion(code => code.Value, value => LocationCode.Create(value).Value)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(w => new { w.TenantId, w.Code }).IsUnique();

        builder.Property(w => w.Name).HasMaxLength(200).IsRequired();
        builder.Property(w => w.OrganizationId).IsRequired();
        builder.Property(w => w.LegalEntityId);

        builder.OwnsOne(w => w.Address, address =>
        {
            address.Property(a => a.Line1).HasColumnName("address_line1").HasMaxLength(200).IsRequired();
            address.Property(a => a.Line2).HasColumnName("address_line2").HasMaxLength(200);
            address.Property(a => a.City).HasColumnName("address_city").HasMaxLength(100).IsRequired();
            address.Property(a => a.ProvinceOrState).HasColumnName("address_province_or_state").HasMaxLength(100);
            address.Property(a => a.PostalCode).HasColumnName("address_postal_code").HasMaxLength(20);
            address.Property(a => a.Country).HasColumnName("address_country").HasMaxLength(100).IsRequired();
        });
        builder.Navigation(w => w.Address).IsRequired();

        builder.Property(w => w.TimeZone)
            .HasConversion(tz => tz.Value, value => WorkLocationTimeZone.Create(value).Value)
            .HasMaxLength(100)
            .IsRequired();

        builder.OwnsOne(w => w.Coordinates, coordinates =>
        {
            coordinates.Property(c => c.Latitude).HasColumnName("latitude");
            coordinates.Property(c => c.Longitude).HasColumnName("longitude");
        });

        builder.Property(w => w.Status).IsRequired();
        builder.Property(w => w.CreatedAtUtc).IsRequired();
    }
}
