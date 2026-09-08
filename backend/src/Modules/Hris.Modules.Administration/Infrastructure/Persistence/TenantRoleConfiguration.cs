using Hris.Infrastructure.Persistence;
using Hris.Modules.Administration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Administration.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="TenantRole"/> Aggregate
/// Root and its own owned <see cref="PermissionGrant"/> collection.
/// </summary>
public sealed class TenantRoleConfiguration : IEntityTypeConfiguration<TenantRole>
{
    public void Configure(EntityTypeBuilder<TenantRole> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("tenant_roles");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasConversion(new StronglyTypedIdValueConverter<TenantRoleId>(value => new TenantRoleId(value)))
            .ValueGeneratedNever();

        builder.Property(r => r.TenantId).IsRequired();
        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(r => new { r.TenantId, r.Name }).IsUnique();

        builder.Property(r => r.Description).HasMaxLength(1000);
        builder.Property(r => r.Status).IsRequired();
        builder.Property(r => r.CreatedBy).IsRequired();
        builder.Property(r => r.CreatedOn).IsRequired();
        builder.Property(r => r.PublishedBy);
        builder.Property(r => r.PublishedOn);

        builder.OwnsMany(r => r.PermissionGrants, ConfigurePermissionGrant);
        builder.Navigation(r => r.PermissionGrants).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigurePermissionGrant(OwnedNavigationBuilder<TenantRole, PermissionGrant> grant)
    {
        grant.ToTable("tenant_role_permission_grants");
        grant.WithOwner().HasForeignKey("TenantRoleId");

        grant.HasKey(g => g.Id);

        grant.Property(g => g.Id)
            .HasConversion(new StronglyTypedIdValueConverter<PermissionGrantId>(value => new PermissionGrantId(value)))
            .ValueGeneratedNever();

        grant.Property(g => g.Permission)
            .HasConversion(permission => permission.Value, value => PermissionReference.Create(value).Value)
            .HasMaxLength(200)
            .IsRequired();

        grant.Property(g => g.AddedBy).IsRequired();
        grant.Property(g => g.AddedOn).IsRequired();
    }
}
