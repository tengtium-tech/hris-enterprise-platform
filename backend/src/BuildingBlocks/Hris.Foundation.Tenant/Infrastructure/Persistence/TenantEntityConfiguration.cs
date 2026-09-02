using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantAggregate = Hris.Foundation.Tenant.Domain.Tenant;

namespace Hris.Foundation.Tenant.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="TenantAggregate"/> Aggregate
/// Root, per coding-standards.md's Infrastructure Layer convention.
///
/// Named <c>TenantEntityConfiguration</c>, not the <c>TenantConfiguration</c> name
/// every sibling framework's own <c>IEntityTypeConfiguration&lt;T&gt;</c> class would
/// otherwise get (<c>CountryConfigurationConfiguration</c>'s own "AggregateName +
/// Configuration" pattern) -- deliberately, because `administration`'s own aggregate
/// is *already* named exactly <c>TenantConfiguration</c> (tenant-configuration.md),
/// a different concept this Tenant Aggregate's own Does Not Own table exists to keep
/// distinct. Reusing that name for an unrelated EF mapping class would recreate, in
/// code, precisely the confusion that table exists to prevent in prose.
///
/// <see cref="Domain.TenantCode"/> is mapped through <c>HasConversion</c>, the same
/// choice <see cref="TenantAggregate"/>'s own constructor was written to keep safe
/// from this Sprint's own EF Core owned-type constructor-binding defect from the
/// start: <see cref="Domain.TenantCode"/> is a scalar-backed Value Object with no
/// owned-type navigation, so <see cref="TenantAggregate"/> needed no second,
/// EF-Core-only constructor -- confirmed via a real EF Core model build before this
/// class was committed (the same scratch-console-harness technique that first found
/// that defect), not assumed from the mapping shape alone.
/// </summary>
public sealed class TenantEntityConfiguration : IEntityTypeConfiguration<TenantAggregate>
{
    public void Configure(EntityTypeBuilder<TenantAggregate> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(tenant => tenant.Id);

        builder.Property(tenant => tenant.Id)
            .HasConversion(new StronglyTypedIdValueConverter<Domain.TenantId>(value => new Domain.TenantId(value)))
            .ValueGeneratedNever();

        builder.Property(tenant => tenant.TenantCode)
            .HasConversion(
                tenantCode => tenantCode.Value,
                value => Domain.TenantCode.Create(value).Value)
            .HasMaxLength(63)
            .IsRequired();

        builder.HasIndex(tenant => tenant.TenantCode).IsUnique();

        builder.Property(tenant => tenant.Organization)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(tenant => tenant.SubscriptionPlan).IsRequired();

        builder.Property(tenant => tenant.LifecycleState).IsRequired();

        builder.HasIndex(tenant => tenant.LifecycleState);
    }
}
