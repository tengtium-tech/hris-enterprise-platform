using Hris.Infrastructure.Persistence;
using Hris.Modules.Organization.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Organization.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="LegalEntity"/> Aggregate
/// Root. LEG-001/LEG-002's own uniqueness (business registration number, tax
/// identification number) is enforced platform-wide here via unfiltered unique
/// indexes -- <see cref="LegalEntity"/> has no <c>TenantId</c>-scoping requirement
/// for either per business-rules.md's own wording ("must be unique," not "unique
/// within a tenant," the distinction ORG-001/ORG-002 draw explicitly for
/// themselves).
/// </summary>
public sealed class LegalEntityConfiguration : IEntityTypeConfiguration<LegalEntity>
{
    public void Configure(EntityTypeBuilder<LegalEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(le => le.Id);

        builder.Property(le => le.Id)
            .HasConversion(new StronglyTypedIdValueConverter<LegalEntityId>(value => new LegalEntityId(value)))
            .ValueGeneratedNever();

        builder.Property(le => le.TenantId).IsRequired();
        builder.HasIndex(le => le.TenantId);

        builder.Property(le => le.Code).HasMaxLength(20).IsRequired();
        builder.Property(le => le.Name).HasMaxLength(200).IsRequired();
        builder.Property(le => le.RegisteredBusinessName).HasMaxLength(200);

        builder.Property(le => le.BusinessRegistrationNumber)
            .HasConversion(reg => reg.Value, value => BusinessRegistrationNumber.Create(value).Value)
            .HasMaxLength(50)
            .IsRequired();
        builder.HasIndex(le => le.BusinessRegistrationNumber).IsUnique();

        builder.Property(le => le.TaxIdentificationNumber)
            .HasConversion(
                tin => tin!.Value,
                value => Domain.TaxIdentificationNumber.Create(value).Value)
            .HasMaxLength(50);
        builder.HasIndex(le => le.TaxIdentificationNumber).IsUnique();

        builder.Property(le => le.Country).HasMaxLength(100).IsRequired();

        builder.Property(le => le.Currency)
            .HasConversion(
                currency => currency!.Value,
                value => CurrencyCode.Create(value).Value)
            .HasMaxLength(3);

        builder.OwnsOne(le => le.RegisteredAddress, address =>
        {
            address.Property(a => a.Line1).HasColumnName("registered_address_line1").HasMaxLength(200);
            address.Property(a => a.Line2).HasColumnName("registered_address_line2").HasMaxLength(200);
            address.Property(a => a.City).HasColumnName("registered_address_city").HasMaxLength(100);
            address.Property(a => a.ProvinceOrState).HasColumnName("registered_address_province_or_state").HasMaxLength(100);
            address.Property(a => a.PostalCode).HasColumnName("registered_address_postal_code").HasMaxLength(20);
            address.Property(a => a.Country).HasColumnName("registered_address_country").HasMaxLength(100);
        });

        builder.Property(le => le.Status).IsRequired();
        builder.Property(le => le.CreatedAtUtc).IsRequired();
    }
}
