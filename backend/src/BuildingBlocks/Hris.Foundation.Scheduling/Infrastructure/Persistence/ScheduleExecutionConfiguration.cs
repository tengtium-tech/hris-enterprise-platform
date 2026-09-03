using Hris.Foundation.Scheduling.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.Scheduling.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="ScheduleExecution"/> Aggregate
/// Root, per coding-standards.md's Infrastructure Layer convention.
/// </summary>
public sealed class ScheduleExecutionConfiguration : IEntityTypeConfiguration<ScheduleExecution>
{
    public void Configure(EntityTypeBuilder<ScheduleExecution> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(execution => execution.Id);

        builder.Property(execution => execution.Id)
            .HasConversion(new StronglyTypedIdValueConverter<ScheduleExecutionId>(value => new ScheduleExecutionId(value)))
            .ValueGeneratedNever();

        builder.Property(execution => execution.ScheduleId)
            .HasConversion(new StronglyTypedIdValueConverter<ScheduleId>(value => new ScheduleId(value)))
            .IsRequired();

        builder.Property(execution => execution.TenantId).IsRequired();

        builder.HasIndex(execution => new { execution.ScheduleId, execution.TenantId, execution.TriggeredAtUtc });

        builder.Property(execution => execution.Status).IsRequired();

        builder.Property(execution => execution.JobIdentifier).HasMaxLength(200);

        builder.Property(execution => execution.RetryCount).IsRequired();

        builder.Property(execution => execution.DurationMs);

        builder.Property(execution => execution.FailureReason).HasMaxLength(500);

        builder.Property(execution => execution.TriggeredAtUtc).IsRequired();

        builder.Property(execution => execution.CompletedAtUtc);
    }
}
