using Hris.Infrastructure.Persistence;
using Hris.Modules.Administration.Domain;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Administration.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="UserAccount"/> Aggregate
/// Root and its own owned <see cref="RoleAssignment"/> collection. Every
/// multi-field Value Object (<see cref="RoleReference"/>, <see cref="OrganizationalScope"/>)
/// nested within the <c>OwnsMany</c> collection is itself mapped <c>OwnsOne</c> --
/// the identical two-level depth (root Aggregate -&gt; owned collection -&gt; nested
/// owned value) <c>Employment</c>'s own <c>CompensationRecord.Amount</c> already
/// proved. <see cref="GrantReason"/> (single-field) is mapped via
/// <c>HasConversion</c> instead, matching <c>EmploymentNumber</c>'s own precedent.
/// </summary>
public sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("user_accounts");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasConversion(new StronglyTypedIdValueConverter<UserAccountId>(value => new UserAccountId(value)))
            .ValueGeneratedNever();

        builder.Property(a => a.TenantId).IsRequired();
        builder.Property(a => a.AccountType).IsRequired();
        builder.Property(a => a.EmployeeId);
        builder.HasIndex(a => new { a.TenantId, a.EmployeeId });

        builder.Property(a => a.Status).IsRequired();
        builder.Property(a => a.ExpiryDate);
        builder.Property(a => a.ServiceAccountOwnerId);
        builder.Property(a => a.ProvisionedBy).IsRequired();
        builder.Property(a => a.ProvisionedOn).IsRequired();

        builder.OwnsMany(a => a.RoleAssignments, ConfigureRoleAssignment);
        builder.Navigation(a => a.RoleAssignments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureRoleAssignment(OwnedNavigationBuilder<UserAccount, RoleAssignment> assignment)
    {
        assignment.ToTable("user_account_role_assignments");
        assignment.WithOwner().HasForeignKey("UserAccountId");

        assignment.HasKey(a => a.Id);

        assignment.Property(a => a.Id)
            .HasConversion(new StronglyTypedIdValueConverter<RoleAssignmentId>(value => new RoleAssignmentId(value)))
            .ValueGeneratedNever();

        assignment.OwnsOne(a => a.Role, role =>
        {
            role.Property(r => r.Kind).HasColumnName("role_kind").IsRequired();
            role.Property(r => r.CanonicalRole).HasColumnName("canonical_role");
            role.Property(r => r.TenantRoleId).HasColumnName("tenant_role_id");
            role.Property(r => r.TenantRoleName).HasColumnName("tenant_role_name").HasMaxLength(100);
        });
        assignment.Navigation(a => a.Role).UsePropertyAccessMode(PropertyAccessMode.Property).IsRequired();

        assignment.OwnsOne(a => a.Scope, scope =>
        {
            scope.Property(s => s.Level).HasColumnName("scope_level").IsRequired();
            scope.Property(s => s.TargetId).HasColumnName("scope_target_id");
        });
        assignment.Navigation(a => a.Scope).UsePropertyAccessMode(PropertyAccessMode.Property).IsRequired();

        assignment.Property(a => a.EffectiveFrom).IsRequired();
        assignment.Property(a => a.EffectiveTo);
        assignment.Property(a => a.GrantedBy).IsRequired();
        assignment.Property(a => a.GrantedOn).IsRequired();

        assignment.Property(a => a.Reason)
            .HasConversion(reason => reason.Value, value => GrantReason.Create(value).Value)
            .HasMaxLength(500)
            .IsRequired();

        assignment.Property(a => a.ApprovalReference);
        assignment.Property(a => a.RevokedBy);
        assignment.Property(a => a.RevokedOn);
        assignment.Property(a => a.IsExpired).IsRequired();

        assignment.HasIndex("UserAccountId", nameof(RoleAssignment.EffectiveFrom));
    }
}
