using Lama.Domain.CustomerManagement.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lama.Infrastructure.Persistence.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.LegalName)
            .HasMaxLength(200);

        builder.Property(o => o.TaxId)
            .HasMaxLength(50);

        builder.Property(o => o.Industry)
            .HasMaxLength(100);

        builder.Property(o => o.Type)
            .IsRequired();

        builder.Property(o => o.Size)
            .IsRequired();

        builder.Property(o => o.Website)
            .HasMaxLength(500);

        builder.Property(o => o.StockSymbol)
            .HasMaxLength(10);

        // Configure self-referencing relationship
        builder.HasOne(o => o.ParentOrganization)
            .WithMany()
            .HasForeignKey(o => o.ParentOrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ignore collections that will be managed separately
        builder.Ignore(o => o.Subsidiaries);
        builder.Ignore(o => o.Accounts);
        builder.Ignore(o => o.Locations);
        builder.Ignore(o => o.CustomFields);
    }
}