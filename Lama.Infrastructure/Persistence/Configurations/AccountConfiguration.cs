using Lama.Domain.CustomerManagement.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lama.Infrastructure.Persistence.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Industry)
            .HasMaxLength(100);

        builder.Property(a => a.Website)
            .HasMaxLength(500);

        builder.Property(a => a.Type)
            .IsRequired();

        // Configure relationship with Contact
        builder.HasMany<Contact>()
            .WithOne()
            .HasForeignKey(c => c.AccountId)
            .OnDelete(DeleteBehavior.SetNull);

        // Ignore the Relationships collection - it will be configured from OrganizationalRelationship side
        builder.Ignore(a => a.Relationships);

        // Ignore the Contacts collection since we're managing it via Contact entity
        builder.Ignore(a => a.Contacts);
    }
}