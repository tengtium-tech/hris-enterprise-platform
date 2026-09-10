using Hris.Infrastructure.Persistence;
using Hris.Modules.Timekeeping.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hris.Modules.Timekeeping.Infrastructure.Persistence;

/// <summary>
/// EF Core configuration for <see cref="HolidayCalendar"/> and its owned
/// <see cref="Holiday"/> collection.
///
/// <c>TenantId</c> is nullable here and required almost everywhere else in the
/// platform, which is deliberate rather than an oversight: the platform-provided
/// Country layer is not tenant data. It belongs to the platform, is read by every
/// tenant operating in that country, and is read-only to all of them (TK-041).
/// </summary>
public sealed class HolidayCalendarConfiguration : IEntityTypeConfiguration<HolidayCalendar>
{
    public void Configure(EntityTypeBuilder<HolidayCalendar> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("holiday_calendars");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasConversion(new StronglyTypedIdValueConverter<HolidayCalendarId>(value => new HolidayCalendarId(value)))
            .ValueGeneratedNever();

        builder.Property(c => c.TenantId);
        builder.Property(c => c.LineageId).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Level).IsRequired();
        builder.Property(c => c.ScopeTargetId).HasMaxLength(200).IsRequired();
        builder.Property(c => c.CountryCode).HasMaxLength(8).IsRequired();

        builder.Property(c => c.ParentCalendarId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? null : new HolidayCalendarId(value.Value));

        builder.Property(c => c.Version).IsRequired();
        builder.Property(c => c.EffectiveFrom).IsRequired();
        builder.Property(c => c.EffectiveTo);
        builder.Property(c => c.Status).IsRequired();
        builder.Property(c => c.CreatedBy).IsRequired();
        builder.Property(c => c.CreatedOn).IsRequired();

        builder.HasIndex(c => new { c.TenantId, c.LineageId });
        builder.HasIndex(c => new { c.Level, c.ScopeTargetId });

        builder.OwnsMany(c => c.Holidays, holiday =>
        {
            holiday.ToTable("holiday_calendar_entries");
            holiday.WithOwner().HasForeignKey("holiday_calendar_id");
            holiday.HasKey(h => h.Id);

            holiday.Property(h => h.Id)
                .HasConversion(new StronglyTypedIdValueConverter<HolidayId>(value => new HolidayId(value)))
                .ValueGeneratedNever();

            holiday.Property(h => h.Date).IsRequired();
            holiday.Property(h => h.Name).HasMaxLength(200).IsRequired();
            holiday.Property(h => h.Type).IsRequired();
            holiday.Property(h => h.WorkRule).IsRequired();

            holiday.HasIndex(h => h.Date);
        });

        builder.Navigation(c => c.Holidays).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
