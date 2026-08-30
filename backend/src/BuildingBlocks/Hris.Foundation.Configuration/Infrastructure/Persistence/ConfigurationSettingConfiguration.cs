using Hris.Foundation.Configuration.Domain;
using Hris.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.Configuration.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="ConfigurationSetting"/>
/// Aggregate Root and its <see cref="ConfigurationVersion"/> child Entity, per
/// coding-standards.md's Infrastructure Layer convention ("EF Core configuration uses
/// Fluent API exclusively") and aggregate-persistence.md ("Only Aggregate Roots are
/// persisted directly... Child Entities exist only within the lifecycle of their
/// owning Aggregate").
///
/// Discovered automatically by <see cref="HrisDbContext.OnModelCreating"/> via
/// <see cref="PersistenceAssemblyRegistry"/> -- see this framework's own
/// <c>DependencyInjection.cs</c> for where this assembly registers itself. Table
/// naming follows database-design-principles.md's stated rule (singular, PascalCase)
/// at the EF model level; the global <c>UseSnakeCaseNamingConvention()</c> configured
/// in <c>Hris.Infrastructure.DependencyInjection</c> lowers that to the physical
/// snake_case identifier naming-conventions.md's own finding recommends -- this class
/// never calls <c>ToTable</c> itself, per dbcontext-design.md's "no inline mapping
/// configuration" read together with "configure [table naming] once, globally."
/// </summary>
public sealed class ConfigurationSettingConfiguration : IEntityTypeConfiguration<ConfigurationSetting>
{
    public void Configure(EntityTypeBuilder<ConfigurationSetting> builder)
    {
        builder.HasKey(setting => setting.Id);

        builder.Property(setting => setting.Id)
            .HasConversion(new StronglyTypedIdValueConverter<ConfigurationId>(value => new ConfigurationId(value)))
            .ValueGeneratedNever();

        // ConfigurationKey: a single-property Value Object, mapped with a Value
        // Converter rather than an Owned Type, per dbcontext-design.md's Value Objects
        // section listing both as valid options -- a converter is the simpler of the
        // two when there is only one underlying column. Round-tripping through
        // ConfigurationKey.Create(...).Value is safe here specifically because nothing
        // reaches this column that did not already pass that same validation on write
        // (CTR-API-003's own "same violation always carries the same stable code"
        // reasoning depends on write-time validation being the only gate, which this
        // preserves).
        builder.Property(setting => setting.Key)
            .HasConversion(
                key => key.Value,
                value => ConfigurationKey.Create(value).Value)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(setting => setting.Key);

        // ConfigurationScope: an Owned Type (aggregate-persistence.md: "persisted with
        // their owning Aggregate... no Repository... cannot exist independently"),
        // stored in the same table as ConfigurationSetting by EF Core's own OwnsOne
        // default -- there is exactly one Scope per Setting, so a separate table would
        // only add a join for no isolation benefit.
        builder.OwnsOne(setting => setting.Scope, scope =>
        {
            scope.Property(s => s.Level)
                .HasColumnName("ScopeLevel")
                .IsRequired();

            scope.Property(s => s.ScopeId)
                .HasColumnName("ScopeId");
        });

        builder.Property(setting => setting.Category).IsRequired();

        builder.Property(setting => setting.DataType).IsRequired();

        // ConfigurationVersion: a child Entity, never an Aggregate Root of its own
        // (see that class's own remarks) -- OwnsMany targeting the public Versions
        // property, with PropertyAccessMode.Field so EF Core reads/writes the private
        // `_versions` backing list directly rather than through the read-only wrapper
        // property, per EF Core's own documented "Backing Fields" support for exactly
        // this DDD encapsulation shape (aggregate-persistence.md's own worked example:
        // "private readonly List<Address> _addresses... public IReadOnlyCollection<Address>
        // Addresses => _addresses").
        builder.OwnsMany(setting => setting.Versions, version =>
        {
            version.WithOwner().HasForeignKey("ConfigurationSettingId");

            version.HasKey(v => v.Id);

            version.Property(v => v.Id)
                .HasConversion(new StronglyTypedIdValueConverter<ConfigurationVersionId>(value => new ConfigurationVersionId(value)))
                .ValueGeneratedNever();

            version.Property(v => v.VersionNumber).IsRequired();
            version.Property(v => v.Value).IsRequired();
            version.Property(v => v.EffectiveDate).IsRequired();
            version.Property(v => v.ExpirationDate);
            version.Property(v => v.ChangeSummary).IsRequired();
            version.Property(v => v.CreatedByUserId).IsRequired();
            version.Property(v => v.State).IsRequired();
            version.Property(v => v.ApprovedByUserId);
            version.Property(v => v.ApprovedAtUtc);
        });

        builder.Navigation(setting => setting.Versions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
