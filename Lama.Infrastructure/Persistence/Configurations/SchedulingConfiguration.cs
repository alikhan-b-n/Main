using Lama.Domain.Scheduling.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lama.Infrastructure.Persistence.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.FullName).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Phone).HasMaxLength(20);
        builder.Property(a => a.TelegramUsername).HasMaxLength(64);

        builder.Property(a => a.StartsAtUtc).IsRequired();
        builder.Property(a => a.DurationMinutes).IsRequired();
        builder.Property(a => a.TimeZoneId).IsRequired().HasMaxLength(64);

        builder.Property(a => a.Kind).IsRequired().HasConversion<string>().HasMaxLength(16);
        builder.Property(a => a.Status).IsRequired().HasConversion<string>().HasMaxLength(16);

        builder.Property(a => a.GoogleEventId).HasMaxLength(1024);
        builder.Property(a => a.MeetUrl).HasMaxLength(512);
        builder.Property(a => a.CalendarLink).HasMaxLength(1024);

        builder.Property(a => a.Comment).HasMaxLength(Appointment.MaxCommentLength);
        builder.Property(a => a.CancelReason).HasMaxLength(Appointment.MaxCommentLength);

        builder.Ignore(a => a.EndsAtUtc);
        builder.Ignore(a => a.IsActive);

        builder.OwnsOne(a => a.Email, email =>
        {
            email.Property(e => e.Value).HasColumnName("Email").HasMaxLength(255).IsRequired();
            email.HasIndex(e => e.Value);
        });

        builder.HasIndex(a => a.StartsAtUtc);
        builder.HasIndex(a => a.LeadId);

        // Two people must not end up in the same slot, however the request arrived
        builder.HasIndex(a => new { a.StartsAtUtc, a.Status })
            .IsUnique()
            .HasFilter("\"Status\" = 'Scheduled'")
            .HasDatabaseName("IX_Appointments_ActiveSlot");
    }
}

public class AvailabilitySettingsConfiguration : IEntityTypeConfiguration<AvailabilitySettings>
{
    public void Configure(EntityTypeBuilder<AvailabilitySettings> builder)
    {
        builder.ToTable("AvailabilitySettings");

        builder.HasKey(s => s.Id);
        // The single row carries a fixed id from the domain
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Enabled).IsRequired();
        builder.Property(s => s.TimeZoneId).IsRequired().HasMaxLength(64);
        builder.Property(s => s.SlotMinutes).IsRequired();
        builder.Property(s => s.MinLeadTimeHours).IsRequired();
        builder.Property(s => s.MaxDaysAhead).IsRequired();
        builder.Property(s => s.BufferMinutes).IsRequired();

        // Working hours are always read and written as a whole, so one JSON column
        // beats a second table with its own migrations
        builder.OwnsMany(s => s.Windows, windows =>
        {
            windows.ToJson("Windows");
            windows.Property(w => w.Day);
            windows.Property(w => w.Start);
            windows.Property(w => w.End);
        });

        builder.Navigation(s => s.Windows).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
