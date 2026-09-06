using Hris.Foundation.Integration.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.Integration.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="Connector"/> Aggregate Root
/// and its owned <see cref="DataMapping"/> child Entity, per coding-standards.md's
/// Infrastructure Layer convention -- the same <c>OwnsMany</c>-with-
/// <c>PropertyAccessMode.Field</c> shape <c>StoredFileConfiguration</c>/
/// <c>DocumentConfiguration</c> already establish for an owned child collection.
///
/// Discovered automatically by <c>HrisDbContext.OnModelCreating</c> via
/// <c>PersistenceAssemblyRegistry</c>.
/// </summary>
public sealed class ConnectorConfiguration : IEntityTypeConfiguration<Connector>
{
    public void Configure(EntityTypeBuilder<Connector> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(connector => connector.Id);

        builder.Property(connector => connector.Id)
            .HasConversion(new StronglyTypedIdValueConverter<ConnectorId>(value => new ConnectorId(value)))
            .ValueGeneratedNever();

        builder.Property(connector => connector.TenantId).IsRequired();
        builder.HasIndex(connector => connector.TenantId);

        builder.Property(connector => connector.Name).HasMaxLength(200).IsRequired();

        builder.Property(connector => connector.IntegrationCategory).HasMaxLength(100).IsRequired();

        builder.Property(connector => connector.EndpointType).IsRequired();

        builder.Property(connector => connector.EndpointAddress).HasMaxLength(1024).IsRequired();

        builder.Property(connector => connector.Status).IsRequired();

        builder.Property(connector => connector.CreatedAtUtc).IsRequired();

        builder.OwnsMany(connector => connector.Mappings, mapping => ConfigureMapping(mapping));
        builder.Navigation(connector => connector.Mappings).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureMapping(OwnedNavigationBuilder<Connector, DataMapping> mapping)
    {
        mapping.ToTable("connector_data_mappings");
        mapping.WithOwner().HasForeignKey("ConnectorId");

        mapping.HasKey(m => m.Id);

        mapping.Property(m => m.Id)
            .HasConversion(new StronglyTypedIdValueConverter<DataMappingId>(value => new DataMappingId(value)))
            .ValueGeneratedNever();

        mapping.Property(m => m.SourceField).HasMaxLength(200).IsRequired();
        mapping.Property(m => m.TargetField).HasMaxLength(200).IsRequired();
        mapping.Property(m => m.TransformationType).IsRequired();
        mapping.Property(m => m.TransformationRule).HasMaxLength(2000);
    }
}
