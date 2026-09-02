using Hris.Foundation.Extension.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.Extension.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="Hook"/> Aggregate Root, per
/// coding-standards.md's Infrastructure Layer convention.
///
/// <see cref="Hook.ExtensionPointId"/> is a plain converted scalar column, never an EF
/// Core navigation/foreign-key relationship to <see cref="ExtensionPoint"/> --
/// <see cref="Hook"/> and <see cref="ExtensionPoint"/> are two independent Aggregate
/// Roots, referenced by id only (<c>CTR-ARC-002</c>), the same "no navigation across
/// an aggregate boundary" rule every cross-aggregate reference in this codebase
/// follows.
/// </summary>
public sealed class HookConfiguration : IEntityTypeConfiguration<Hook>
{
    public void Configure(EntityTypeBuilder<Hook> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(hook => hook.Id);

        builder.Property(hook => hook.Id)
            .HasConversion(new StronglyTypedIdValueConverter<HookId>(value => new HookId(value)))
            .ValueGeneratedNever();

        builder.Property(hook => hook.ExtensionPointId)
            .HasConversion(new StronglyTypedIdValueConverter<ExtensionPointId>(value => new ExtensionPointId(value)))
            .IsRequired();

        builder.HasIndex(hook => hook.ExtensionPointId);

        builder.Property(hook => hook.HookType).IsRequired();

        builder.Property(hook => hook.HandlerReference)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(hook => hook.OwningModule)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(hook => hook.Status).IsRequired();
    }
}
