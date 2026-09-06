using Hris.Foundation.Integration.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.Integration.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="IntegrationRun"/> Aggregate
/// Root, per coding-standards.md's Infrastructure Layer convention. A plain top-level
/// table, not an owned type of <see cref="Connector"/> -- see
/// <see cref="IntegrationRun"/>'s own remarks for why it is its own,
/// population-scale Aggregate Root.
///
/// Discovered automatically by <c>HrisDbContext.OnModelCreating</c> via
/// <c>PersistenceAssemblyRegistry</c>.
/// </summary>
public sealed class IntegrationRunConfiguration : IEntityTypeConfiguration<IntegrationRun>
{
    public void Configure(EntityTypeBuilder<IntegrationRun> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(run => run.Id);

        builder.Property(run => run.Id)
            .HasConversion(new StronglyTypedIdValueConverter<IntegrationRunId>(value => new IntegrationRunId(value)))
            .ValueGeneratedNever();

        builder.Property(run => run.TenantId).IsRequired();

        builder.Property(run => run.ConnectorId)
            .HasConversion(new StronglyTypedIdValueConverter<ConnectorId>(value => new ConnectorId(value)))
            .IsRequired();

        builder.HasIndex(run => new { run.TenantId, run.ConnectorId });

        builder.Property(run => run.RunKind).IsRequired();

        builder.Property(run => run.SyncModel).HasMaxLength(100);

        builder.Property(run => run.Status).IsRequired();

        builder.Property(run => run.StartedAtUtc).IsRequired();

        builder.Property(run => run.FailureReason).HasMaxLength(2000);

        builder.Property(run => run.RetryCount).IsRequired();

        builder.Property(run => run.MaxRetries).IsRequired();
    }
}
