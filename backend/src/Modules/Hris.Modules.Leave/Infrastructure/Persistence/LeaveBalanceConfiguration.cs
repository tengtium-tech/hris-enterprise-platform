using Hris.Infrastructure.Persistence;
using Hris.Modules.Leave.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Leave.Infrastructure.Persistence;

/// <summary>
/// EF Core configuration for <see cref="LeaveBalance"/> and its owned
/// <see cref="LeaveLedgerEntry"/> collection. The ledger table is granted no
/// <c>UPDATE</c>/<c>DELETE</c> permission at the database level — enforcing LV-020
/// structurally, not only through the Application layer's own absence of any command
/// that could mutate an entry — see the migration that creates it. Source:
/// docs/04-modules/leave/domain/aggregates.md (LeaveBalance) and entities.md
/// (LeaveLedgerEntry).
/// </summary>
public sealed class LeaveBalanceConfiguration : IEntityTypeConfiguration<LeaveBalance>
{
    public void Configure(EntityTypeBuilder<LeaveBalance> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("leave_balances");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .HasConversion(new StronglyTypedIdValueConverter<LeaveBalanceId>(value => new LeaveBalanceId(value)))
            .ValueGeneratedNever();

        builder.Property(b => b.TenantId).IsRequired();
        builder.Property(b => b.EmployeeId).IsRequired();
        builder.Property(b => b.LeaveTypeId)
            .HasConversion(new StronglyTypedIdValueConverter<LeaveTypeId>(value => new LeaveTypeId(value)))
            .IsRequired();
        builder.Property(b => b.CurrentBalance).HasColumnType("decimal(9,2)").IsRequired();
        builder.Property(b => b.LastRecalculatedAt);

        builder.HasIndex(b => new { b.TenantId, b.EmployeeId, b.LeaveTypeId }).IsUnique();

        builder.OwnsMany(b => b.LedgerEntries, entry =>
        {
            entry.ToTable("leave_ledger_entries");
            entry.WithOwner().HasForeignKey("leave_balance_id");
            entry.HasKey(e => e.Id);

            entry.Property(e => e.Id)
                .HasConversion(new StronglyTypedIdValueConverter<LeaveLedgerEntryId>(value => new LeaveLedgerEntryId(value)))
                .ValueGeneratedNever();

            entry.Property(e => e.EntryType).IsRequired();
            entry.Property(e => e.Amount).HasColumnType("decimal(9,2)").IsRequired();
            entry.Property(e => e.EffectiveDate).IsRequired();
            entry.Property(e => e.SourceReference).IsRequired();
            entry.Property(e => e.Actor);
            entry.Property(e => e.RecordedAt).IsRequired();

            entry.HasIndex("leave_balance_id", nameof(LeaveLedgerEntry.RecordedAt));
        });
        builder.Navigation(b => b.LedgerEntries).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
