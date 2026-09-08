using Hris.Infrastructure.Persistence;
using Hris.Modules.Position.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Position.Infrastructure.Persistence;

public sealed class JobClassificationConfiguration : IEntityTypeConfiguration<JobClassification>
{
    public void Configure(EntityTypeBuilder<JobClassification> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasConversion(new StronglyTypedIdValueConverter<JobClassificationId>(value => new JobClassificationId(value)))
            .ValueGeneratedNever();

        builder.Property(c => c.TenantId).IsRequired();
        builder.HasIndex(c => c.TenantId);

        builder.Property(c => c.Code)
            .HasConversion(code => code.Value, value => JobClassificationCode.Create(value).Value)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(c => new { c.TenantId, c.Code }).IsUnique();

        builder.Property(c => c.Name)
            .HasConversion(name => name.Value, value => JobClassificationName.Create(value).Value)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(c => new { c.TenantId, c.Name }).IsUnique();

        builder.Property(c => c.Description).HasMaxLength(1000);
        builder.Property(c => c.Status).IsRequired();
        builder.Property(c => c.CreatedAtUtc).IsRequired();
    }
}
