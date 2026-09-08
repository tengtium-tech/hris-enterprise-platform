using Hris.Infrastructure.Persistence;
using Hris.Modules.Employment.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Employment.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="EmploymentContract"/>
/// Aggregate Root and its own owned child collections (ContractRenewal,
/// ContractExtension, ContractDocument), per infrastructure/persistence.md's own
/// Table Mapping section. Discovered automatically by
/// <c>HrisDbContext.OnModelCreating</c> via <c>PersistenceAssemblyRegistry</c>.
/// </summary>
public sealed class EmploymentContractConfiguration : IEntityTypeConfiguration<EmploymentContract>
{
    public void Configure(EntityTypeBuilder<EmploymentContract> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("employment_contracts");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasConversion(new StronglyTypedIdValueConverter<EmploymentContractId>(value => new EmploymentContractId(value)))
            .ValueGeneratedNever();

        builder.Property(c => c.TenantId).IsRequired();
        builder.Property(c => c.EmploymentId).IsRequired();
        builder.HasIndex(c => c.EmploymentId);

        builder.Property(c => c.ContractType)
            .HasConversion(type => type.Value, value => Domain.ContractType.Create(value).Value)
            .HasMaxLength(100)
            .IsRequired();

        builder.OwnsOne(c => c.Period, period =>
        {
            period.Property(p => p.StartDate).HasColumnName("period_start_date").IsRequired();
            period.Property(p => p.EndDate).HasColumnName("period_end_date");
        });
        builder.Navigation(c => c.Period).UsePropertyAccessMode(PropertyAccessMode.Property).IsRequired();

        builder.Property(c => c.LifecycleStage).IsRequired();
        builder.Property(c => c.SupersedesContractId);
        builder.Property(c => c.CreatedAtUtc).IsRequired();

        builder.OwnsMany(c => c.Renewals, ConfigureRenewal);
        builder.Navigation(c => c.Renewals).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(c => c.Extensions, ConfigureExtension);
        builder.Navigation(c => c.Extensions).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(c => c.Documents, ConfigureDocument);
        builder.Navigation(c => c.Documents).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureRenewal(OwnedNavigationBuilder<EmploymentContract, ContractRenewal> renewal)
    {
        renewal.ToTable("employment_contract_renewals");
        renewal.WithOwner().HasForeignKey("EmploymentContractId");

        renewal.HasKey(r => r.Id);

        renewal.Property(r => r.Id)
            .HasConversion(new StronglyTypedIdValueConverter<ContractRenewalId>(value => new ContractRenewalId(value)))
            .ValueGeneratedNever();

        renewal.Property(r => r.PreviousStartDate).IsRequired();
        renewal.Property(r => r.PreviousEndDate);
        renewal.Property(r => r.NewStartDate).IsRequired();
        renewal.Property(r => r.NewEndDate);
        renewal.Property(r => r.ApprovalReference).HasMaxLength(200);
        renewal.Property(r => r.CreatedAtUtc).IsRequired();
    }

    private static void ConfigureExtension(OwnedNavigationBuilder<EmploymentContract, ContractExtension> extension)
    {
        extension.ToTable("employment_contract_extensions");
        extension.WithOwner().HasForeignKey("EmploymentContractId");

        extension.HasKey(e => e.Id);

        extension.Property(e => e.Id)
            .HasConversion(new StronglyTypedIdValueConverter<ContractExtensionId>(value => new ContractExtensionId(value)))
            .ValueGeneratedNever();

        extension.Property(e => e.PreviousEndDate).IsRequired();
        extension.Property(e => e.NewEndDate).IsRequired();
        extension.Property(e => e.Reason).HasMaxLength(1000);
        extension.Property(e => e.CreatedAtUtc).IsRequired();
    }

    private static void ConfigureDocument(OwnedNavigationBuilder<EmploymentContract, ContractDocument> document)
    {
        document.ToTable("employment_contract_documents");
        document.WithOwner().HasForeignKey("EmploymentContractId");

        document.HasKey(d => d.Id);

        document.Property(d => d.Id)
            .HasConversion(new StronglyTypedIdValueConverter<ContractDocumentId>(value => new ContractDocumentId(value)))
            .ValueGeneratedNever();

        document.Property(d => d.DocumentType).HasMaxLength(100).IsRequired();
        document.Property(d => d.StorageReference).HasMaxLength(500).IsRequired();
        document.Property(d => d.Version).IsRequired();
        document.Property(d => d.CreatedAtUtc).IsRequired();
    }
}
