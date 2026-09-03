using Hris.Foundation.JobProcessing.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.JobProcessing.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="JobQueue"/> Aggregate Root, per
/// coding-standards.md's Infrastructure Layer convention. No owned type or Complex Type
/// anywhere in this shape -- every property is a scalar or a single-column
/// <c>HasConversion</c> Value Object, and <see cref="JobQueue"/>'s own constructor
/// avoided the property-name-mismatch pitfall Search Framework's own
/// <c>IndexedDocument</c>/<c>SearchExecution</c>/<c>SavedSearch</c> each needed a
/// second constructor for, the same discipline <c>Schedule</c> already applies.
/// </summary>
public sealed class JobQueueConfiguration : IEntityTypeConfiguration<JobQueue>
{
    public void Configure(EntityTypeBuilder<JobQueue> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(jobQueue => jobQueue.Id);

        builder.Property(jobQueue => jobQueue.Id)
            .HasConversion(new StronglyTypedIdValueConverter<JobQueueId>(value => new JobQueueId(value)))
            .ValueGeneratedNever();

        builder.Property(jobQueue => jobQueue.Name)
            .HasConversion(
                name => name.Value,
                value => JobQueueName.Create(value).Value)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(jobQueue => jobQueue.Name).IsUnique();

        builder.Property(jobQueue => jobQueue.MaxConcurrency).IsRequired();

        builder.Property(jobQueue => jobQueue.DefaultMaxRetries).IsRequired();

        builder.Property(jobQueue => jobQueue.DefaultRetryDelaySeconds).IsRequired();

        builder.Property(jobQueue => jobQueue.CreatedAtUtc).IsRequired();
    }
}
