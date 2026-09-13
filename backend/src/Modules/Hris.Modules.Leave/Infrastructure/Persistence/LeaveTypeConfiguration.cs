using Hris.Infrastructure.Persistence;
using Hris.Modules.Leave.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Leave.Infrastructure.Persistence;

/// <summary>
/// EF Core configuration for <see cref="LeaveType"/>. <c>TenantId</c> is nullable —
/// platform-scope statutory rows carry none, mirroring Timekeeping's own
/// <c>HolidayCalendar</c> Country-layer treatment (infrastructure/persistence.md).
/// </summary>
public sealed class LeaveTypeConfiguration : IEntityTypeConfiguration<LeaveType>
{
    public void Configure(EntityTypeBuilder<LeaveType> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("leave_types");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasConversion(new StronglyTypedIdValueConverter<LeaveTypeId>(value => new LeaveTypeId(value)))
            .ValueGeneratedNever();

        builder.Property(t => t.TenantId);
        builder.Property(t => t.Code).HasMaxLength(50).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Category).IsRequired();
        builder.Property(t => t.Scope).IsRequired();
        builder.Property(t => t.StatutoryBasis).HasMaxLength(200);
        builder.Property(t => t.StatutoryMinimum).HasColumnType("decimal(9,2)");
        builder.Property(t => t.Status).IsRequired();

        builder.HasIndex(t => new { t.TenantId, t.Code });
    }
}
