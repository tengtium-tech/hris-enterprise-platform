using Hris.Foundation.JobProcessing.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.JobProcessing.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="Worker"/> Aggregate Root, per
/// coding-standards.md's Infrastructure Layer convention.
/// </summary>
public sealed class WorkerConfiguration : IEntityTypeConfiguration<Worker>
{
    public void Configure(EntityTypeBuilder<Worker> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(worker => worker.Id);

        builder.Property(worker => worker.Id)
            .HasConversion(new StronglyTypedIdValueConverter<WorkerId>(value => new WorkerId(value)))
            .ValueGeneratedNever();

        builder.Property(worker => worker.InstanceId).HasMaxLength(200).IsRequired();

        builder.Property(worker => worker.Status).IsRequired();

        builder.Property(worker => worker.StartedAtUtc).IsRequired();

        builder.Property(worker => worker.StoppedAtUtc);
    }
}
