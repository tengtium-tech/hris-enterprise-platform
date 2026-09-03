using Hris.Foundation.FileStorage.Domain;
using Hris.Foundation.Identity.Domain;
using Hris.Infrastructure.Persistence;
using Hris.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Foundation.FileStorage.Infrastructure.Persistence;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="StoredFile"/> Aggregate Root and
/// its owned <see cref="FileVersion"/> child Entity, per coding-standards.md's
/// Infrastructure Layer convention -- the same <c>OwnsMany</c>-with-<c>PropertyAccessMode.Field</c>
/// shape <c>RuleDefinitionConfiguration</c> already establishes for <c>RuleVersion</c>.
///
/// One case that shape alone does not cover: <see cref="StoredFile.PendingVersion"/> and
/// <see cref="StoredFile.Versions"/> are both <see cref="FileVersion"/>, but EF Core's
/// owned-type model requires every owned instance to have exactly one owner navigation --
/// a version cannot simultaneously live in the <c>OwnsMany</c> history table and a
/// second, independent <c>OwnsOne</c> table while it is the *same row*. It never needs
/// to be: <see cref="StoredFile.PendingVersion"/> and a promoted, historical version are
/// always different rows in this framework's own domain model (<c>MarkStored</c> clears
/// <c>_pendingVersion</c> in the same step it appends the promoted version to
/// <c>_versions</c>), so mapping them as two separate owned tables -- one <c>OwnsOne</c>,
/// one <c>OwnsMany</c> -- is correct, not a workaround. EF Core's own default for an
/// optional <c>OwnsOne</c> is table splitting -- sharing the owner's row -- which a real
/// model build confirmed conflicts with the explicit foreign key below the moment two
/// owned navigations target the same table; each is given its own explicit
/// <c>ToTable</c> name for exactly this reason, found empirically rather than
/// anticipated. <see cref="ConfigureFileVersion"/> is factored out once and reused for
/// both, since <c>OwnsOne</c> and <c>OwnsMany</c> share the identical
/// <see cref="OwnedNavigationBuilder{TEntity,TDependentEntity}"/> callback shape and
/// every column mapping below applies to both tables identically.
///
/// <see cref="FileVersion.Checksum"/> is mapped through a single-column
/// <c>HasConversion</c>, via its own <see cref="Checksum.ToString"/>/parse round-trip,
/// rather than a nested <c>OwnsOne</c> -- the same "prefer a converter over a nested
/// owned type for a Value Object with no independent query need of its own component
/// parts" choice <c>ExtensionPointConfiguration</c> already makes for
/// <c>SupportedHookTypes</c>.
///
/// Discovered automatically by <c>HrisDbContext.OnModelCreating</c> via
/// <c>PersistenceAssemblyRegistry</c>.
/// </summary>
public sealed class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(storedFile => storedFile.Id);

        builder.Property(storedFile => storedFile.Id)
            .HasConversion(new StronglyTypedIdValueConverter<StoredFileId>(value => new StoredFileId(value)))
            .ValueGeneratedNever();

        builder.Property(storedFile => storedFile.ContainerName)
            .HasConversion(
                containerName => containerName.Value,
                value => ContainerName.Create(value).Value)
            .HasMaxLength(63)
            .IsRequired();

        builder.HasIndex(storedFile => storedFile.ContainerName);

        builder.Property(storedFile => storedFile.OriginalFileName)
            .HasMaxLength(260)
            .IsRequired();

        builder.Property(storedFile => storedFile.Status).IsRequired();

        // EF Core's own default for an optional OwnsOne is table splitting -- sharing
        // the owner's own table -- which collides with the explicit foreign key
        // ConfigureFileVersion already gives it (StoredFileConfiguration's own remarks
        // above explain why PendingVersion needs a real, separate row rather than a
        // shared one). An explicit ToTable forces its own table, confirmed necessary by
        // this exact model-build failure -- EF's own validator names the conflict by
        // table, not by navigation, so this was found empirically, not anticipated.
        // Explicit table names are taken as literal by the snake_case naming
        // convention plugin -- it only transforms convention-derived names -- so both
        // are spelled out in the same snake_case every other, convention-derived table
        // and column in this schema already uses, confirmed by the scratch model-build
        // harness rendering an un-cased "StoredFilePendingVersions" before this was set
        // explicitly.
        builder.OwnsOne(storedFile => storedFile.PendingVersion, version => ConfigureFileVersion(version, "stored_file_pending_versions"));
        builder.Navigation(storedFile => storedFile.PendingVersion).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(storedFile => storedFile.Versions, version => ConfigureFileVersion(version, "stored_file_versions"));
        builder.Navigation(storedFile => storedFile.Versions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureFileVersion(OwnedNavigationBuilder<StoredFile, FileVersion> version, string tableName)
    {
        version.ToTable(tableName);
        version.WithOwner().HasForeignKey("StoredFileId");

        version.HasKey(v => v.Id);

        version.Property(v => v.Id)
            .HasConversion(new StronglyTypedIdValueConverter<FileVersionId>(value => new FileVersionId(value)))
            .ValueGeneratedNever();

        version.Property(v => v.VersionNumber).IsRequired();

        // No explicit HasColumnName on these three -- naming-conventions.md's Table
        // Naming finding requires the snake_case convention configured once, globally,
        // in HrisDbContext.OnModelCreating to govern every column; an explicit column
        // name here would silently override it for these three alone, confirmed by the
        // scratch model-build harness rendering them PascalCase before this was fixed.
        version.Property(v => v.StorageObjectKey)
            .HasConversion(
                key => key.Value,
                value => StorageObjectKey.Create(value).Value)
            .HasMaxLength(1024)
            .IsRequired();

        version.Property(v => v.Checksum)
            .HasConversion(
                checksum => checksum.ToString(),
                value => ParseChecksum(value))
            .HasMaxLength(80)
            .IsRequired();

        version.Property(v => v.FileSizeBytes).IsRequired();

        version.Property(v => v.MimeType)
            .HasConversion(
                mimeType => mimeType.Value,
                value => MimeType.Create(value).Value)
            .HasMaxLength(255)
            .IsRequired();

        version.Property(v => v.StorageProviderType).IsRequired();

        version.Property(v => v.UploadedByUserId)
            .HasConversion(new StronglyTypedIdValueConverter<UserAccountId>(value => new UserAccountId(value)))
            .IsRequired();

        version.Property(v => v.UploadedAtUtc).IsRequired();
    }

    private static Checksum ParseChecksum(string value)
    {
        var parts = value.Split(':', 2);
        var algorithm = Enum.Parse<ChecksumAlgorithm>(parts[0]);
        return Checksum.Create(algorithm, parts[1]).Value;
    }
}
