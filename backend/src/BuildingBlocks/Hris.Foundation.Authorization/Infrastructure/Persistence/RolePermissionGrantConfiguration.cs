using Hris.Foundation.Authorization.Domain;
using Hris.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.Authorization.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="RolePermissionGrant"/>
/// Aggregate Root. Discovered automatically by
/// <see cref="HrisDbContext.OnModelCreating"/> via <see cref="PersistenceAssemblyRegistry"/>.
/// </summary>
public sealed class RolePermissionGrantConfiguration : IEntityTypeConfiguration<RolePermissionGrant>
{
    public void Configure(EntityTypeBuilder<RolePermissionGrant> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(grant => grant.Id);

        builder.Property(grant => grant.Id)
            .HasConversion(new StronglyTypedIdValueConverter<RolePermissionGrantId>(value => new RolePermissionGrantId(value)))
            .ValueGeneratedNever();

        // AuthorizationEvaluator's own GetActiveGrantsForRolesAsync reads by Role for
        // every evaluation -- the same NFR-PF-001 indexing reasoning
        // RoleAssignmentConfiguration states for its own PrincipalId index.
        builder.HasIndex(grant => grant.Role);

        builder.Property(grant => grant.Role).IsRequired();

        // PermissionKey: an Owned Type, stored in the same table -- exactly one
        // permission per grant.
        builder.OwnsOne(grant => grant.Permission, permission =>
        {
            permission.Property(p => p.ResourceType).HasColumnName("ResourceType").HasMaxLength(200).IsRequired();
            permission.Property(p => p.Action).HasColumnName("Action").IsRequired();
        });

        builder.Property(grant => grant.GrantedAtUtc).IsRequired();
        builder.Property(grant => grant.RevokedAtUtc);
    }
}
