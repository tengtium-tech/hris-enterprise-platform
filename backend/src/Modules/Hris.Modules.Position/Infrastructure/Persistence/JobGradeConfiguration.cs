using Hris.Infrastructure.Persistence;
using Hris.Modules.Position.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Position.Infrastructure.Persistence;

public sealed class JobGradeConfiguration : IEntityTypeConfiguration<JobGrade>
{
    public void Configure(EntityTypeBuilder<JobGrade> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Id)
            .HasConversion(new StronglyTypedIdValueConverter<JobGradeId>(value => new JobGradeId(value)))
            .ValueGeneratedNever();

        builder.Property(g => g.TenantId).IsRequired();
        builder.HasIndex(g => g.TenantId);

        builder.Property(g => g.Code)
            .HasConversion(code => code.Value, value => JobGradeCode.Create(value).Value)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(g => new { g.TenantId, g.Code }).IsUnique();

        builder.Property(g => g.Name)
            .HasConversion(name => name.Value, value => JobGradeName.Create(value).Value)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(g => new { g.TenantId, g.Name }).IsUnique();

        builder.Property(g => g.Description).HasMaxLength(1000);
        builder.Property(g => g.OrganizationalLevel);
        builder.Property(g => g.Status).IsRequired();
        builder.Property(g => g.CreatedAtUtc).IsRequired();
    }
}
