using Hris.Infrastructure.Persistence;
using Hris.Modules.Position.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Position.Infrastructure.Persistence;

public sealed class JobFamilyConfiguration : IEntityTypeConfiguration<JobFamily>
{
    public void Configure(EntityTypeBuilder<JobFamily> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .HasConversion(new StronglyTypedIdValueConverter<JobFamilyId>(value => new JobFamilyId(value)))
            .ValueGeneratedNever();

        builder.Property(f => f.TenantId).IsRequired();
        builder.HasIndex(f => f.TenantId);

        builder.Property(f => f.Code)
            .HasConversion(code => code.Value, value => JobFamilyCode.Create(value).Value)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(f => new { f.TenantId, f.Code }).IsUnique();

        builder.Property(f => f.Name)
            .HasConversion(name => name.Value, value => JobFamilyName.Create(value).Value)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(f => new { f.TenantId, f.Name }).IsUnique();

        builder.Property(f => f.Description).HasMaxLength(1000);
        builder.Property(f => f.Status).IsRequired();
        builder.Property(f => f.CreatedAtUtc).IsRequired();
    }
}
