using Lama.Domain.SalesManagement.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lama.Infrastructure.Persistence.Configurations;

public class SalesForecastConfiguration : IEntityTypeConfiguration<SalesForecast>
{
    public void Configure(EntityTypeBuilder<SalesForecast> builder)
    {
        builder.HasKey(sf => sf.Id);

        builder.Property(sf => sf.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(sf => sf.Period)
            .IsRequired();

        builder.Property(sf => sf.StartDate)
            .IsRequired();

        builder.Property(sf => sf.EndDate)
            .IsRequired();

        builder.Property(sf => sf.Status)
            .IsRequired();

        builder.Property(sf => sf.ConfidenceLevel)
            .HasPrecision(5, 2);

        // Ignore the LineItems collection - it's a value object collection
        builder.Ignore(sf => sf.LineItems);
    }
}