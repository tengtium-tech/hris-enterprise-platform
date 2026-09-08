using Hris.Infrastructure.Persistence;
using Hris.Modules.Employee.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Employee.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="EmployeeHistory"/> Aggregate
/// Root -- a flat, append-only table with no owned child collections.
/// </summary>
public sealed class EmployeeHistoryConfiguration : IEntityTypeConfiguration<EmployeeHistory>
{
    public void Configure(EntityTypeBuilder<EmployeeHistory> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("employee_history");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id)
            .HasConversion(new StronglyTypedIdValueConverter<EmployeeHistoryId>(value => new EmployeeHistoryId(value)))
            .ValueGeneratedNever();

        builder.Property(h => h.TenantId).IsRequired();
        builder.Property(h => h.EmployeeId).IsRequired();
        builder.HasIndex(h => new { h.TenantId, h.EmployeeId });

        builder.Property(h => h.Category).IsRequired();
        builder.Property(h => h.PreviousValue).HasMaxLength(2000);
        builder.Property(h => h.NewValue).HasMaxLength(2000);
        builder.Property(h => h.EffectiveDate).IsRequired();
        builder.Property(h => h.BusinessReason).HasMaxLength(1000);
        builder.Property(h => h.ChangedBy);
        builder.Property(h => h.CreatedAtUtc).IsRequired();
    }
}
