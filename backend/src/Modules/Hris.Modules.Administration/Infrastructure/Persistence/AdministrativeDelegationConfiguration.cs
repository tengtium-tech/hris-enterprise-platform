using System.Text.Json;
using Hris.Infrastructure.Persistence;
using Hris.Modules.Administration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Administration.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="AdministrativeDelegation"/>
/// Aggregate Root. <see cref="AdministrativeDelegation.DelegatedAuthority"/> is
/// mapped as a single JSON-serialized column via <c>HasConversion</c>, not an
/// owned collection -- see that property's own remarks for why. EF Core cannot
/// compare two deserialized list instances by reference for change tracking, so
/// a value comparer based on the serialized JSON is supplied alongside the
/// conversion, the standard EF Core pattern for a converted collection property.
/// </summary>
public sealed class AdministrativeDelegationConfiguration : IEntityTypeConfiguration<AdministrativeDelegation>
{
    public void Configure(EntityTypeBuilder<AdministrativeDelegation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("administrative_delegations");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasConversion(new StronglyTypedIdValueConverter<DelegationId>(value => new DelegationId(value)))
            .ValueGeneratedNever();

        builder.Property(d => d.TenantId).IsRequired();
        builder.Property(d => d.DelegatorUserAccountId).IsRequired();
        builder.Property(d => d.DelegateUserAccountId).IsRequired();
        builder.HasIndex(d => new { d.TenantId, d.DelegatorUserAccountId });
        builder.HasIndex(d => new { d.TenantId, d.DelegateUserAccountId });

        var delegatedAuthorityComparer = new ValueComparer<IReadOnlyList<DelegatedAuthorityItem>>(
            (left, right) => JsonSerializer.Serialize(left, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(right, (JsonSerializerOptions?)null),
            items => JsonSerializer.Serialize(items, (JsonSerializerOptions?)null).GetHashCode(StringComparison.Ordinal),
            items => (IReadOnlyList<DelegatedAuthorityItem>)items.ToList());

        builder.Property(d => d.DelegatedAuthority)
            .HasConversion(
                items => JsonSerializer.Serialize(items, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<DelegatedAuthorityItem>>(json, (JsonSerializerOptions?)null) ?? new List<DelegatedAuthorityItem>())
            .Metadata.SetValueComparer(delegatedAuthorityComparer);

        builder.OwnsOne(d => d.Period, period =>
        {
            period.Property(p => p.Start).HasColumnName("period_start").IsRequired();
            period.Property(p => p.End).HasColumnName("period_end").IsRequired();
        });
        builder.Navigation(d => d.Period).UsePropertyAccessMode(PropertyAccessMode.Property).IsRequired();

        builder.Property(d => d.Reason).HasMaxLength(500).IsRequired();
        builder.Property(d => d.ApprovalReference);
        builder.Property(d => d.Status).IsRequired();
        builder.Property(d => d.CreatedOn).IsRequired();
    }
}
