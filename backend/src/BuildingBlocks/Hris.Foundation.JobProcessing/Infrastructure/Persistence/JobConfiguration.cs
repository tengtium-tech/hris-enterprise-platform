using Hris.Foundation.JobProcessing.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.JobProcessing.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="Job"/> Aggregate Root, per
/// coding-standards.md's Infrastructure Layer convention. The composite index below
/// backs <see cref="IJobRepository.ListByQueueAsync"/>'s own history query --
/// <see cref="Job.TenantId"/> leads it for the same reason it leads
/// <c>IndexedDocumentConfiguration</c>'s own index (<c>CTR-ISO-004</c>).
/// </summary>
public sealed class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(job => job.Id);

        builder.Property(job => job.Id)
            .HasConversion(new StronglyTypedIdValueConverter<JobId>(value => new JobId(value)))
            .ValueGeneratedNever();

        builder.Property(job => job.TenantId).IsRequired();

        builder.Property(job => job.JobType).HasMaxLength(200).IsRequired();

        builder.Property(job => job.JobQueueId)
            .HasConversion(new StronglyTypedIdValueConverter<JobQueueId>(value => new JobQueueId(value)))
            .IsRequired();

        builder.HasIndex(job => new { job.TenantId, job.JobQueueId, job.SubmittedAtUtc });

        builder.Property(job => job.Priority).IsRequired();

        builder.Property(job => job.PayloadReference).HasMaxLength(2000);

        builder.Property(job => job.SubmittedByUserId);

        builder.Property(job => job.Status).IsRequired();

        builder.Property(job => job.RetryCount).IsRequired();

        builder.Property(job => job.MaxRetries).IsRequired();

        builder.Property(job => job.FailureReason).HasMaxLength(2000);

        builder.Property(job => job.SubmittedAtUtc).IsRequired();

        builder.Property(job => job.StartedAtUtc);

        builder.Property(job => job.CompletedAtUtc);
    }
}
