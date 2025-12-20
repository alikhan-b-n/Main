using Lama.Domain.CustomerService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lama.Infrastructure.Persistence.Configurations;

public class ServiceWorkflowConfiguration : IEntityTypeConfiguration<ServiceWorkflow>
{
    public void Configure(EntityTypeBuilder<ServiceWorkflow> builder)
    {
        builder.HasKey(sw => sw.Id);

        builder.Property(sw => sw.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(sw => sw.Description)
            .HasMaxLength(1000);

        builder.Property(sw => sw.IsActive)
            .IsRequired();

        // Ignore the Steps collection - it's a value object collection
        builder.Ignore(sw => sw.Steps);
    }
}