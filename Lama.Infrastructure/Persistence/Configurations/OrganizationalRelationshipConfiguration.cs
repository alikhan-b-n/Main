using Lama.Domain.CustomerManagement.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lama.Infrastructure.Persistence.Configurations;

public class OrganizationalRelationshipConfiguration : IEntityTypeConfiguration<OrganizationalRelationship>
{
    public void Configure(EntityTypeBuilder<OrganizationalRelationship> builder)
    {
        builder.HasKey(or => or.Id);

        builder.Property(or => or.Type)
            .IsRequired();

        builder.Property(or => or.Description)
            .HasMaxLength(500);

        // Configure relationship with source account
        builder.HasOne(or => or.SourceAccount)
            .WithMany()
            .HasForeignKey(or => or.SourceAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure relationship with target account
        builder.HasOne(or => or.TargetAccount)
            .WithMany()
            .HasForeignKey(or => or.TargetAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}