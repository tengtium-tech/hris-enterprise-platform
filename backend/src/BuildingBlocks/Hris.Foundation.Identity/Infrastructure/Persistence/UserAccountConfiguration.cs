using Hris.Foundation.Identity.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.Identity.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="UserAccount"/> Aggregate Root
/// and its <see cref="Session"/>/<see cref="MfaFactor"/> child Entities, per
/// coding-standards.md's Infrastructure Layer convention and aggregate-persistence.md
/// -- the identical shape <c>ConfigurationSettingConfiguration</c> establishes for
/// <c>ConfigurationSetting</c>/<c>ConfigurationVersion</c>; see that class's own
/// remarks for the reasoning behind Value Converters vs. Owned Types, backing-field
/// access for owned collections, and never calling <c>ToTable</c> here.
///
/// Discovered automatically by <see cref="HrisDbContext.OnModelCreating"/> via
/// <see cref="PersistenceAssemblyRegistry"/> -- see this framework's own
/// <c>DependencyInjection.cs</c> for where this assembly registers itself.
/// </summary>
public sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(account => account.Id);

        builder.Property(account => account.Id)
            .HasConversion(new StronglyTypedIdValueConverter<UserAccountId>(value => new UserAccountId(value)))
            .ValueGeneratedNever();

        builder.Property(account => account.TenantId).IsRequired();

        // Username: a single-property Value Object, mapped with a Value Converter --
        // the same choice ConfigurationSettingConfiguration makes for ConfigurationKey,
        // for the same reason (a converter is simpler than an Owned Type when there is
        // only one underlying column). Uniqueness is enforced per-tenant, not
        // platform-wide -- identity-framework.md's own Zero Trust/tenant-scoping
        // principles mean two different tenants' own users may coincidentally choose
        // the same handle without conflict.
        builder.Property(account => account.Username)
            .HasConversion(
                username => username.Value,
                value => Username.Create(value).Value)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(account => new { account.TenantId, account.Username }).IsUnique();

        builder.Property(account => account.EmailAddress)
            .HasConversion(
                email => email.Value,
                value => EmailAddress.Create(value).Value)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(account => account.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(account => account.IdentityType).IsRequired();

        builder.Property(account => account.LinkedIdentityId);

        builder.Property(account => account.Status).IsRequired();

        // AuthenticationProvider: an Owned Type, stored in the same table as
        // UserAccount by EF Core's own OwnsOne default -- there is exactly one
        // provider per account, so a separate table would only add a join.
        builder.OwnsOne(account => account.AuthenticationProvider, provider =>
        {
            provider.Property(p => p.Key)
                .HasColumnName("AuthenticationProviderKey")
                .HasMaxLength(100)
                .IsRequired();
        });

        // PasswordHash: nullable -- an Invited or federated account genuinely has none
        // yet, per that Value Object's own remarks. ToString() is overridden to
        // "***REDACTED***" specifically so it can never leak into a log line, but that
        // has no bearing here: this converter reads/writes the real digest directly
        // via .Value, the one place round-tripping the actual credential is correct.
        builder.Property(account => account.PasswordHash)
            .HasConversion(
                hash => hash != null ? hash.Value : null,
                value => value != null ? PasswordHash.Create(value).Value : null)
            .HasMaxLength(500);

        builder.Property(account => account.FailedAuthenticationAttemptCount).IsRequired();

        builder.Property(account => account.LastLoginAtUtc);

        // Session: a child Entity, never its own Aggregate Root (see that class's own
        // remarks) -- OwnsMany targeting the public Sessions property, with
        // PropertyAccessMode.Field so EF Core reads/writes the private `_sessions`
        // backing list, the identical pattern ConfigurationSettingConfiguration uses
        // for ConfigurationVersion.
        builder.OwnsMany(account => account.Sessions, session =>
        {
            session.WithOwner().HasForeignKey("UserAccountId");

            session.HasKey(s => s.Id);

            session.Property(s => s.Id)
                .HasConversion(new StronglyTypedIdValueConverter<SessionId>(value => new SessionId(value)))
                .ValueGeneratedNever();

            session.Property(s => s.TenantId).IsRequired();
            session.Property(s => s.DeviceLabel).HasMaxLength(200).IsRequired();
            session.Property(s => s.ApproximateLocation).HasMaxLength(200);
            session.Property(s => s.CreatedAtUtc).IsRequired();
            session.Property(s => s.ExpiresAtUtc).IsRequired();
            session.Property(s => s.LastActiveAtUtc).IsRequired();
            session.Property(s => s.RevokedAtUtc);
        });

        builder.Navigation(account => account.Sessions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // MfaFactor: a child Entity, the same shape as Session above.
        builder.OwnsMany(account => account.MfaFactors, factor =>
        {
            factor.WithOwner().HasForeignKey("UserAccountId");

            factor.HasKey(f => f.Id);

            factor.Property(f => f.Id)
                .HasConversion(new StronglyTypedIdValueConverter<MfaFactorId>(value => new MfaFactorId(value)))
                .ValueGeneratedNever();

            factor.Property(f => f.FactorType).IsRequired();
            factor.Property(f => f.SecretReference).HasMaxLength(500).IsRequired();
            factor.Property(f => f.EnrolledAtUtc).IsRequired();
            factor.Property(f => f.RemovedAtUtc);
        });

        builder.Navigation(account => account.MfaFactors)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
