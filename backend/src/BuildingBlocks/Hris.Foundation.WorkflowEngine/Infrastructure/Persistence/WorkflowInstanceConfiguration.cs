using Hris.Foundation.WorkflowEngine.Domain;
using Hris.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.WorkflowEngine.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="WorkflowInstance"/> Aggregate
/// Root, per coding-standards.md's Infrastructure Layer convention. No owned type or
/// Complex Type anywhere in this shape, and every constructor parameter already shares
/// its name with the property it sets (see that class's own remarks), so this
/// configuration needs no second, EF-materialization-only constructor -- confirmed by a
/// real model build, not assumed.
/// </summary>
public sealed class WorkflowInstanceConfiguration : IEntityTypeConfiguration<WorkflowInstance>
{
    public void Configure(EntityTypeBuilder<WorkflowInstance> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(instance => instance.Id);

        builder.Property(instance => instance.Id)
            .HasConversion(new StronglyTypedIdValueConverter<WorkflowInstanceId>(value => new WorkflowInstanceId(value)))
            .ValueGeneratedNever();

        builder.Property(instance => instance.TenantId).IsRequired();

        builder.Property(instance => instance.WorkflowDefinitionId)
            .HasConversion(new StronglyTypedIdValueConverter<WorkflowDefinitionId>(value => new WorkflowDefinitionId(value)))
            .IsRequired();

        builder.HasIndex(instance => new { instance.WorkflowDefinitionId, instance.TenantId });

        builder.Property(instance => instance.WorkflowDefinitionVersionNumber).IsRequired();

        builder.Property(instance => instance.TriggeringReference).HasMaxLength(200);

        builder.Property(instance => instance.InitiatedByUserId).IsRequired();

        builder.Property(instance => instance.Status).IsRequired();

        builder.Property(instance => instance.CurrentStepOrder).IsRequired();

        builder.Property(instance => instance.StartedAtUtc).IsRequired();

        builder.Property(instance => instance.CompletedAtUtc);

        builder.Property(instance => instance.FailureReason).HasMaxLength(2000);
    }
}
