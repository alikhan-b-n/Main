using Lama.Domain.CustomerService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lama.Infrastructure.Persistence.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TicketName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(t => t.Priority)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(t => t.Source)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(t => t.ContactId)
            .IsRequired();

        builder.Property(t => t.PipelineId)
            .IsRequired();

        builder.Property(t => t.StageId)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.UpdatedAt);

        builder.Property(t => t.ClosedAt);

        builder.HasIndex(t => t.ContactId);
        builder.HasIndex(t => t.CompanyId);
        builder.HasIndex(t => t.OwnerId);
        builder.HasIndex(t => t.PipelineId);
        builder.HasIndex(t => t.StageId);
        builder.HasIndex(t => t.Status);
    }
}
