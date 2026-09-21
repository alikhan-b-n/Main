using Lama.Domain.LeadManagement.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lama.Infrastructure.Persistence.Configurations;

public class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    private const int CodeLength = 64;
    private const int FreeTextLength = 300;

    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ToTable("Leads");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.FullName).IsRequired().HasMaxLength(100);
        builder.Property(l => l.Phone).HasMaxLength(20);
        builder.Property(l => l.TelegramUsername).HasMaxLength(CodeLength);

        builder.Property(l => l.Source).IsRequired().HasMaxLength(CodeLength);
        builder.Property(l => l.ExternalId).HasMaxLength(CodeLength);

        builder.Property(l => l.ApplicantType).HasMaxLength(CodeLength);
        builder.Property(l => l.AgeRange).HasMaxLength(CodeLength);
        builder.Property(l => l.CurrentEducation).HasMaxLength(CodeLength);
        builder.Property(l => l.TargetDegree).HasMaxLength(CodeLength);
        builder.Property(l => l.IntakeYear).HasMaxLength(CodeLength);
        builder.Property(l => l.FundingNeed).HasMaxLength(CodeLength);
        builder.Property(l => l.AnnualBudget).HasMaxLength(CodeLength);
        builder.Property(l => l.EnglishLevel).HasMaxLength(CodeLength);
        builder.Property(l => l.EnglishCertificate).HasMaxLength(CodeLength);
        builder.Property(l => l.Gpa).HasMaxLength(FreeTextLength);
        builder.Property(l => l.EnglishScore).HasMaxLength(FreeTextLength);
        builder.Property(l => l.FieldsOfInterest).HasMaxLength(FreeTextLength);
        builder.Property(l => l.UniversityPriority).HasMaxLength(CodeLength);
        builder.Property(l => l.NextStep).HasMaxLength(16);

        // Mapped to PostgreSQL text[] so the list can be filtered by country
        builder.Property(l => l.TargetCountries).IsRequired();
        builder.Property(l => l.ServicesNeeded).IsRequired();

        builder.Property(l => l.Status).IsRequired().HasConversion<string>().HasMaxLength(32);
        builder.Property(l => l.Temperature).IsRequired().HasConversion<string>().HasMaxLength(16);
        builder.Property(l => l.LostReason).HasMaxLength(500);

        builder.Property(l => l.SurveySummary).HasMaxLength(4000);
        builder.Property(l => l.RawPayload).HasColumnType("jsonb");
        builder.Property(l => l.SubmittedAt).IsRequired();
        builder.Property(l => l.LastActivityAt).IsRequired();
        builder.Property(l => l.CreatedAt).IsRequired();
        builder.Property(l => l.UpdatedAt);

        builder.Ignore(l => l.IsOpen);

        builder.HasMany(l => l.Events)
            .WithOne()
            .HasForeignKey(e => e.LeadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(l => l.Events).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(l => l.Status);
        builder.HasIndex(l => l.CreatedAt);
        builder.HasIndex(l => l.Source);
        builder.HasIndex(l => l.TelegramId);
        builder.HasIndex(l => l.ExternalId).IsUnique();
    }
}

public class LeadEventConfiguration : IEntityTypeConfiguration<LeadEvent>
{
    public void Configure(EntityTypeBuilder<LeadEvent> builder)
    {
        builder.ToTable("LeadEvents");

        builder.HasKey(e => e.Id);

        // Ids are assigned in the domain. Without this, an event added to a loaded lead
        // would be treated as an existing row and EF would try to UPDATE it.
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Kind).IsRequired().HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.FromStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.ToStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Text).HasMaxLength(LeadEvent.MaxTextLength);
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasIndex(e => new { e.LeadId, e.CreatedAt });
    }
}
