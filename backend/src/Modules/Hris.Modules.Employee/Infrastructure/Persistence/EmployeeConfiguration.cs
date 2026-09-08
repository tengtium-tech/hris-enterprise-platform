using Hris.Infrastructure.Persistence;
using Hris.Modules.Employee.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Employee.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="Domain.Employee"/> Aggregate
/// Root and its own owned child collections (EmergencyContact, FamilyMember), per
/// infrastructure/persistence.md's own Table Mapping section -- the same
/// <c>OwnsMany</c>/<c>HasConversion</c> shape <c>EmploymentConfiguration</c> already
/// establishes. Every multi-field Value Object (<see cref="PersonName"/>,
/// <see cref="Domain.Address"/>, <see cref="BankAccount"/>) is mapped
/// <c>OwnsOne</c> exactly one level deep, matching the proven depth
/// <c>EmploymentContract.Period</c> and <c>CompensationRecord.Amount</c> already
/// establish; every single-field Value Object is mapped via <c>HasConversion</c>
/// instead, matching <c>EmploymentNumber</c>'s own precedent -- neither goes any
/// deeper, since this Sprint has no live Postgres to verify a nested-owned-type
/// configuration against beyond the Model-build smoke test. Discovered
/// automatically by <c>HrisDbContext.OnModelCreating</c> via
/// <c>PersistenceAssemblyRegistry</c>.
/// </summary>
public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Domain.Employee>
{
    public void Configure(EntityTypeBuilder<Domain.Employee> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("employees");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(new StronglyTypedIdValueConverter<EmployeeId>(value => new EmployeeId(value)))
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId).IsRequired();

        builder.Property(e => e.Number)
            .HasConversion(number => number.Value, value => EmployeeNumber.Create(value).Value)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.Number }).IsUnique();

        builder.OwnsOne(e => e.Name, name =>
        {
            name.Property(n => n.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
            name.Property(n => n.MiddleName).HasColumnName("middle_name").HasMaxLength(100);
            name.Property(n => n.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
            name.Property(n => n.Prefix).HasColumnName("name_prefix").HasMaxLength(20);
            name.Property(n => n.Suffix).HasColumnName("name_suffix").HasMaxLength(20);
            name.Property(n => n.PreferredName).HasColumnName("preferred_name").HasMaxLength(100);
        });
        builder.Navigation(e => e.Name).UsePropertyAccessMode(PropertyAccessMode.Property).IsRequired();

        builder.Property(e => e.DateOfBirth).IsRequired();
        builder.Property(e => e.BirthPlace).HasMaxLength(200);
        builder.Property(e => e.Gender).IsRequired();
        builder.Property(e => e.CivilStatus).IsRequired();
        builder.Property(e => e.Nationality).HasMaxLength(100);
        builder.Property(e => e.Citizenship).HasMaxLength(100);
        builder.Property(e => e.PhotographReference);
        builder.Property(e => e.SignatureReference);

        builder.Property(e => e.PersonalEmail)
            .HasConversion(
                email => email == null ? null : email.Value,
                value => value == null ? null : EmailAddress.Create(value).Value)
            .HasMaxLength(254);

        builder.Property(e => e.CompanyEmail)
            .HasConversion(
                email => email == null ? null : email.Value,
                value => value == null ? null : EmailAddress.Create(value).Value)
            .HasMaxLength(254);

        builder.Property(e => e.MobileNumber)
            .HasConversion(
                phone => phone == null ? null : phone.Value,
                value => value == null ? null : PhoneNumber.Create(value).Value)
            .HasMaxLength(30);

        builder.Property(e => e.TelephoneNumber)
            .HasConversion(
                phone => phone == null ? null : phone.Value,
                value => value == null ? null : PhoneNumber.Create(value).Value)
            .HasMaxLength(30);

        builder.OwnsOne(e => e.HomeAddress, address => ConfigureAddress(address, "home_"));
        builder.Navigation(e => e.HomeAddress).UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.OwnsOne(e => e.MailingAddress, address => ConfigureAddress(address, "mailing_"));
        builder.Navigation(e => e.MailingAddress).UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property(e => e.Tin)
            .HasConversion(tin => tin == null ? null : tin.Value, value => value == null ? null : Domain.Tin.Create(value).Value)
            .HasMaxLength(20)
            .HasColumnName("tin");

        builder.Property(e => e.Sss)
            .HasConversion(sss => sss == null ? null : sss.Value, value => value == null ? null : SssNumber.Create(value).Value)
            .HasMaxLength(20)
            .HasColumnName("sss_number");

        builder.Property(e => e.PhilHealth)
            .HasConversion(
                philHealth => philHealth == null ? null : philHealth.Value,
                value => value == null ? null : PhilHealthNumber.Create(value).Value)
            .HasMaxLength(20)
            .HasColumnName("philhealth_number");

        builder.Property(e => e.PagIbig)
            .HasConversion(
                pagIbig => pagIbig == null ? null : pagIbig.Value,
                value => value == null ? null : PagIbigNumber.Create(value).Value)
            .HasMaxLength(20)
            .HasColumnName("pagibig_number");

        builder.Property(e => e.Gsis)
            .HasConversion(gsis => gsis == null ? null : gsis.Value, value => value == null ? null : GsisNumber.Create(value).Value)
            .HasMaxLength(20)
            .HasColumnName("gsis_number");

        builder.OwnsOne(e => e.Banking, banking =>
        {
            banking.Property(b => b.BankName).HasColumnName("bank_name").HasMaxLength(100);
            banking.Property(b => b.Branch).HasColumnName("bank_branch").HasMaxLength(100);
            banking.Property(b => b.AccountName).HasColumnName("bank_account_name").HasMaxLength(100);
            banking.Property(b => b.AccountNumber).HasColumnName("bank_account_number").HasMaxLength(100);
            banking.Property(b => b.SwiftCode).HasColumnName("bank_swift_code").HasMaxLength(20);
        });
        builder.Navigation(e => e.Banking).UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property(e => e.LifecycleStage).IsRequired();
        builder.Property(e => e.CreatedAtUtc).IsRequired();

        builder.OwnsMany(e => e.EmergencyContacts, ConfigureEmergencyContact);
        builder.Navigation(e => e.EmergencyContacts).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(e => e.FamilyMembers, ConfigureFamilyMember);
        builder.Navigation(e => e.FamilyMembers).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureAddress(OwnedNavigationBuilder<Domain.Employee, Domain.Address> address, string columnPrefix)
    {
        address.Property(a => a.AddressLine1).HasColumnName(columnPrefix + "address_line1").HasMaxLength(200);
        address.Property(a => a.AddressLine2).HasColumnName(columnPrefix + "address_line2").HasMaxLength(200);
        address.Property(a => a.City).HasColumnName(columnPrefix + "city").HasMaxLength(100);
        address.Property(a => a.Province).HasColumnName(columnPrefix + "province").HasMaxLength(100);
        address.Property(a => a.PostalCode).HasColumnName(columnPrefix + "postal_code").HasMaxLength(20);
        address.Property(a => a.Country).HasColumnName(columnPrefix + "country").HasMaxLength(100);
    }

    private static void ConfigureEmergencyContact(OwnedNavigationBuilder<Domain.Employee, EmergencyContact> contact)
    {
        contact.ToTable("employee_emergency_contacts");
        contact.WithOwner().HasForeignKey("EmployeeId");

        contact.HasKey(c => c.Id);

        contact.Property(c => c.Id)
            .HasConversion(new StronglyTypedIdValueConverter<EmergencyContactId>(value => new EmergencyContactId(value)))
            .ValueGeneratedNever();

        contact.Property(c => c.Name).HasMaxLength(100).IsRequired();
        contact.Property(c => c.Relationship).HasMaxLength(50).IsRequired();

        contact.Property(c => c.Phone)
            .HasConversion(phone => phone.Value, value => PhoneNumber.Create(value).Value)
            .HasMaxLength(30)
            .IsRequired();

        contact.Property(c => c.Email)
            .HasConversion(
                email => email == null ? null : email.Value,
                value => value == null ? null : EmailAddress.Create(value).Value)
            .HasMaxLength(254);

        contact.Property(c => c.IsPrimary).IsRequired();
        contact.Property(c => c.CreatedAtUtc).IsRequired();
    }

    private static void ConfigureFamilyMember(OwnedNavigationBuilder<Domain.Employee, FamilyMember> member)
    {
        member.ToTable("employee_family_members");
        member.WithOwner().HasForeignKey("EmployeeId");

        member.HasKey(m => m.Id);

        member.Property(m => m.Id)
            .HasConversion(new StronglyTypedIdValueConverter<FamilyMemberId>(value => new FamilyMemberId(value)))
            .ValueGeneratedNever();

        member.Property(m => m.Name).HasMaxLength(100).IsRequired();
        member.Property(m => m.Relationship).IsRequired();
        member.Property(m => m.DateOfBirth);
        member.Property(m => m.IsDependent).IsRequired();
        member.Property(m => m.CreatedAtUtc).IsRequired();
    }
}
