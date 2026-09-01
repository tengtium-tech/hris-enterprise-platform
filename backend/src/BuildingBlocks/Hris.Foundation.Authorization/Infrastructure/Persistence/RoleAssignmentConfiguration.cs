using Hris.Foundation.Authorization.Domain;
using Hris.Foundation.Identity.Domain;
using Hris.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.Authorization.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="RoleAssignment"/> Aggregate
/// Root, per coding-standards.md's Infrastructure Layer convention -- the identical
/// shape <c>UserAccountConfiguration</c> already establishes.
///
/// Discovered automatically by <see cref="HrisDbContext.OnModelCreating"/> via
/// <see cref="PersistenceAssemblyRegistry"/>.
/// </summary>
public sealed class RoleAssignmentConfiguration : IEntityTypeConfiguration<RoleAssignment>
{
    public void Configure(EntityTypeBuilder<RoleAssignment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(assignment => assignment.Id);

        builder.Property(assignment => assignment.Id)
            .HasConversion(new StronglyTypedIdValueConverter<RoleAssignmentId>(value => new RoleAssignmentId(value)))
            .ValueGeneratedNever();

        // PrincipalId: a nullable-elsewhere Strongly Typed Id here is non-nullable, so
        // the shared non-nullable converter applies directly, unlike EventEnvelope's
        // own Actor column.
        builder.Property(assignment => assignment.PrincipalId)
            .HasConversion(new StronglyTypedIdValueConverter<UserAccountId>(value => new UserAccountId(value)))
            .IsRequired();

        // `CTR-AUT-007` ("Revocation Takes Effect Immediately") depends on
        // AuthorizationEvaluator always reading a fresh set of assignments for a
        // principal -- this index is what makes that read fast rather than a full
        // table scan on every evaluation, per this framework's own NFR-PF-001.
        builder.HasIndex(assignment => assignment.PrincipalId);

        builder.Property(assignment => assignment.Role).IsRequired();

        // OrganizationalScope: an Owned Type, stored in the same table -- the same
        // choice ConfigurationSettingConfiguration makes for ConfigurationScope, for
        // the same reason (exactly one scope per assignment).
        builder.OwnsOne(assignment => assignment.Scope, scope =>
        {
            scope.Property(s => s.Level).HasColumnName("ScopeLevel").IsRequired();
            scope.Property(s => s.ScopeId).HasColumnName("ScopeId").IsRequired();
        });

        builder.Property(assignment => assignment.AssignmentType).IsRequired();
        builder.Property(assignment => assignment.EffectiveDate).IsRequired();
        builder.Property(assignment => assignment.ExpirationDate);

        builder.Property(assignment => assignment.GrantedByPrincipalId)
            .HasConversion(new StronglyTypedIdValueConverter<UserAccountId>(value => new UserAccountId(value)))
            .IsRequired();

        builder.Property(assignment => assignment.GrantedAtUtc).IsRequired();
        builder.Property(assignment => assignment.RevokedAtUtc);
    }
}
