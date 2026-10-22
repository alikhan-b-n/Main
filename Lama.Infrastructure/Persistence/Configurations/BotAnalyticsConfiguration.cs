using Lama.Domain.BotAnalytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lama.Infrastructure.Persistence.Configurations;

public class SurveyEventConfiguration : IEntityTypeConfiguration<SurveyEvent>
{
    public void Configure(EntityTypeBuilder<SurveyEvent> builder)
    {
        builder.ToTable("SurveyEvents");

        builder.HasKey(e => e.Id);
        // The bot picks the id, which is what makes a resent batch harmless
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.SessionId).IsRequired().HasMaxLength(SurveyEvent.MaxCodeLength);
        builder.Property(e => e.TelegramUsername).HasMaxLength(SurveyEvent.MaxCodeLength);
        builder.Property(e => e.TelegramName).HasMaxLength(SurveyEvent.MaxNameLength);
        builder.Property(e => e.Source).HasMaxLength(SurveyEvent.MaxCodeLength);
        builder.Property(e => e.Kind).IsRequired().HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.QuestionId).HasMaxLength(SurveyEvent.MaxCodeLength);
        builder.Property(e => e.Detail).HasMaxLength(SurveyEvent.MaxCodeLength);
        // Stored without a zone (legacy timestamps); read back as UTC so the API says "Z"
        builder.Property(e => e.OccurredAt).IsRequired()
            .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        // The page always reads a time window
        builder.HasIndex(e => e.OccurredAt);
        builder.HasIndex(e => e.SessionId);
    }
}

public class BotReminderSettingsConfiguration : IEntityTypeConfiguration<BotReminderSettings>
{
    public void Configure(EntityTypeBuilder<BotReminderSettings> builder)
    {
        builder.ToTable("BotReminderSettings");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Enabled).IsRequired();
        builder.Property(s => s.DelayHours).IsRequired();
        builder.Property(s => s.QuietFrom).IsRequired();
        builder.Property(s => s.QuietTo).IsRequired();
        builder.Property(s => s.TimeZoneId).IsRequired().HasMaxLength(64);
    }
}
