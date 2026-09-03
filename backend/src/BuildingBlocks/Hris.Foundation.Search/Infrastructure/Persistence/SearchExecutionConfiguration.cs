using Hris.Foundation.Search.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.Search.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="SearchExecution"/> Aggregate
/// Root, per coding-standards.md's Infrastructure Layer convention.
/// </summary>
public sealed class SearchExecutionConfiguration : IEntityTypeConfiguration<SearchExecution>
{
    public void Configure(EntityTypeBuilder<SearchExecution> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(execution => execution.Id);

        builder.Property(execution => execution.Id)
            .HasConversion(new StronglyTypedIdValueConverter<SearchExecutionId>(value => new SearchExecutionId(value)))
            .ValueGeneratedNever();

        builder.Property(execution => execution.TenantId).IsRequired();

        builder.HasIndex(execution => new { execution.TenantId, execution.RequestedAtUtc });

        builder.Property(execution => execution.RequestedByUserId).IsRequired();

        builder.Property(execution => execution.QueryText).HasMaxLength(500).IsRequired();

        builder.Property(execution => execution.DomainFilter).HasMaxLength(100);

        builder.Property(execution => execution.Status).IsRequired();

        builder.Property(execution => execution.ResultCount);

        builder.Property(execution => execution.LatencyMs);

        builder.Property(execution => execution.FailureReason).HasMaxLength(500);

        builder.Property(execution => execution.RequestedAtUtc).IsRequired();

        builder.Property(execution => execution.CompletedAtUtc);
    }
}
