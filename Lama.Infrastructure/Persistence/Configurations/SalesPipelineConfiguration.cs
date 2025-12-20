using Lama.Domain.SalesManagement.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lama.Infrastructure.Persistence.Configurations;

public class SalesPipelineConfiguration : IEntityTypeConfiguration<SalesPipeline>
{
    public void Configure(EntityTypeBuilder<SalesPipeline> builder)
    {
        builder.HasKey(sp => sp.Id);

        builder.Property(sp => sp.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(sp => sp.Description)
            .HasMaxLength(1000);

        builder.Property(sp => sp.IsActive)
            .IsRequired();

        // Ignore the Opportunities collection
        builder.Ignore(sp => sp.Opportunities);
    }
}