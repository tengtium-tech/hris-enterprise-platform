using System.Text.Json;
using Hris.Infrastructure.Persistence;
using Hris.Modules.Workflow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Workflow.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="ApprovalDelegation"/>
/// Aggregate Root. The table is deliberately separate from administration's own
/// <c>administrative_delegations</c>: the two are different arrangements that both
/// exist (WR-044), and a shared table would be the first step toward the merge that
/// document prohibits.
/// </summary>
public sealed class ApprovalDelegationConfiguration : IEntityTypeConfiguration<ApprovalDelegation>
{
    public void Configure(EntityTypeBuilder<ApprovalDelegation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("approval_delegations");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasConversion(new StronglyTypedIdValueConverter<ApprovalDelegationId>(value => new ApprovalDelegationId(value)))
            .ValueGeneratedNever();

        builder.Property(d => d.TenantId).IsRequired();
        builder.Property(d => d.DelegatorUserAccountId).IsRequired();
        builder.Property(d => d.DelegateUserAccountId).IsRequired();
        builder.HasIndex(d => new { d.TenantId, d.DelegatorUserAccountId });
        builder.HasIndex(d => new { d.TenantId, d.DelegateUserAccountId });

        var scopeComparer = new ValueComparer<IReadOnlyList<Guid>>(
            (left, right) => JsonSerializer.Serialize(left, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(right, (JsonSerializerOptions?)null),
            items => JsonSerializer.Serialize(items, (JsonSerializerOptions?)null).GetHashCode(StringComparison.Ordinal),
            items => (IReadOnlyList<Guid>)items.ToList());

        builder.Property(d => d.Scope)
            .HasColumnName("scope")
            .HasConversion(
                items => JsonSerializer.Serialize(items, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<Guid>>(json, (JsonSerializerOptions?)null) ?? new List<Guid>())
            .Metadata.SetValueComparer(scopeComparer);

        builder.Property(d => d.CoversAllProcesses).IsRequired();

        builder.OwnsOne(d => d.Period, period =>
        {
            period.Property(p => p.Start).HasColumnName("period_start").IsRequired();
            period.Property(p => p.End).HasColumnName("period_end").IsRequired();
        });
        builder.Navigation(d => d.Period).UsePropertyAccessMode(PropertyAccessMode.Property).IsRequired();

        builder.Property(d => d.Reason).HasMaxLength(500).IsRequired();
        builder.Property(d => d.ApprovalReference);
        builder.Property(d => d.Status).IsRequired();
        builder.Property(d => d.CreatedBy).IsRequired();
        builder.Property(d => d.CreatedOn).IsRequired();
        builder.Property(d => d.RevokedBy);
        builder.Property(d => d.RevokedOn);
    }
}
