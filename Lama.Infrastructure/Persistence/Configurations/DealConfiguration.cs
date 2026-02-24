using Lama.Domain.SalesManagement.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lama.Infrastructure.Persistence.Configurations;

public class DealConfiguration : IEntityTypeConfiguration<Deal>
{
    public void Configure(EntityTypeBuilder<Deal> builder)
    {
        builder.ToTable("Deals");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.Description)
            .HasMaxLength(2000);

        builder.Property(d => d.Probability)
            .IsRequired();

        builder.Property(d => d.ExpectedCloseDate)
            .IsRequired();

        builder.Property(d => d.ActualCloseDate);

        builder.Property(d => d.CompanyId)
            .IsRequired();

        builder.Property(d => d.PipelineId)
            .IsRequired();

        builder.Property(d => d.StageId)
            .IsRequired();

        builder.Property(d => d.CreatedAt)
            .IsRequired();

        builder.Property(d => d.UpdatedAt);

        builder.HasIndex(d => d.CompanyId);
        builder.HasIndex(d => d.ContactId);
        builder.HasIndex(d => d.OwnerId);
        builder.HasIndex(d => d.PipelineId);
        builder.HasIndex(d => d.StageId);
    }
}
