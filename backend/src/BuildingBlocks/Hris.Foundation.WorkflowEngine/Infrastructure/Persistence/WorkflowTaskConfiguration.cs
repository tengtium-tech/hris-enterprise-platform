using Hris.Foundation.WorkflowEngine.Domain;
using Hris.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.WorkflowEngine.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="WorkflowTask"/> Aggregate Root,
/// per coding-standards.md's Infrastructure Layer convention. No owned type or Complex
/// Type anywhere in this shape, and every constructor parameter already shares its name
/// with the property it sets, so this configuration needs no second,
/// EF-materialization-only constructor -- confirmed by a real model build, not assumed.
/// </summary>
public sealed class WorkflowTaskConfiguration : IEntityTypeConfiguration<WorkflowTask>
{
    public void Configure(EntityTypeBuilder<WorkflowTask> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(task => task.Id);

        builder.Property(task => task.Id)
            .HasConversion(new StronglyTypedIdValueConverter<WorkflowTaskId>(value => new WorkflowTaskId(value)))
            .ValueGeneratedNever();

        builder.Property(task => task.TenantId).IsRequired();

        builder.Property(task => task.WorkflowInstanceId)
            .HasConversion(new StronglyTypedIdValueConverter<WorkflowInstanceId>(value => new WorkflowInstanceId(value)))
            .IsRequired();

        builder.HasIndex(task => task.WorkflowInstanceId);

        builder.HasIndex(task => new { task.AssignedToUserId, task.TenantId, task.Status });

        builder.Property(task => task.StepName).HasMaxLength(200).IsRequired();

        builder.Property(task => task.StepOrder).IsRequired();

        builder.Property(task => task.ParticipantType).IsRequired();

        builder.Property(task => task.ParticipantRoleName).HasMaxLength(50);

        builder.Property(task => task.AssignedToUserId);

        builder.Property(task => task.Status).IsRequired();

        builder.Property(task => task.Comments).HasMaxLength(2000);

        builder.Property(task => task.DelegatedToUserId);

        builder.Property(task => task.EscalationLevel).IsRequired();

        builder.Property(task => task.CreatedAtUtc).IsRequired();

        builder.Property(task => task.CompletedAtUtc);
    }
}
