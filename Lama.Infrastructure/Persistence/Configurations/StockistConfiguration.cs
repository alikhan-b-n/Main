using Lama.Domain.Stockists.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lama.Infrastructure.Persistence.Configurations;

public class StockistConfiguration : IEntityTypeConfiguration<Stockist>
{
    public void Configure(EntityTypeBuilder<Stockist> builder)
    {
        builder.ToTable("Stockists");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(Stockist.MaxNameLength);
        builder.Property(s => s.Region).HasMaxLength(Stockist.MaxRegionLength);
        builder.Property(s => s.Address).HasMaxLength(Stockist.MaxAddressLength);
        builder.Property(s => s.ContactName).HasMaxLength(Stockist.MaxContactNameLength);
        builder.Property(s => s.Email).HasMaxLength(Stockist.MaxEmailLength);
        builder.Property(s => s.Phone).HasMaxLength(Stockist.MaxPhoneLength);
        builder.Property(s => s.Stage).IsRequired().HasConversion<string>().HasMaxLength(16);
        builder.Property(s => s.LastContactedOn);
        builder.Property(s => s.LastActivityAt).IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();

        builder.Ignore(s => s.IsStore);
        builder.Ignore(s => s.TotalBooks);

        // One row per title the store has had; the key keeps a title from appearing twice
        builder.OwnsMany(s => s.Stock, stock =>
        {
            stock.ToTable("StockistStock");
            stock.WithOwner().HasForeignKey("StockistId");
            stock.HasKey("StockistId", nameof(StockLevel.Title));
            stock.Property(l => l.Title).HasMaxLength(32);
            stock.Property(l => l.Quantity).IsRequired();
        });
        builder.Navigation(s => s.Stock).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(s => s.Invoices)
            .WithOne()
            .HasForeignKey(i => i.StockistId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.Invoices).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(s => s.Events)
            .WithOne()
            .HasForeignKey(e => e.StockistId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.Events).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(s => s.Name);
        builder.HasIndex(s => s.Stage);
    }
}

public class StockistInvoiceConfiguration : IEntityTypeConfiguration<StockistInvoice>
{
    public void Configure(EntityTypeBuilder<StockistInvoice> builder)
    {
        builder.ToTable("StockistInvoices");

        builder.HasKey(i => i.Id);

        // Ids are assigned in the domain. Without this, an invoice added to a loaded store
        // would be treated as an existing row and EF would try to UPDATE it.
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.SentOn).IsRequired();
        builder.Property(i => i.Amount).HasPrecision(18, 2);
        builder.Property(i => i.Reference).HasMaxLength(StockistInvoice.MaxReferenceLength);
        builder.Property(i => i.CreatedAt).IsRequired();

        builder.Ignore(i => i.IsPaid);

        builder.HasIndex(i => new { i.StockistId, i.SentOn });
    }
}

public class StockistEventConfiguration : IEntityTypeConfiguration<StockistEvent>
{
    public void Configure(EntityTypeBuilder<StockistEvent> builder)
    {
        builder.ToTable("StockistEvents");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Kind).IsRequired().HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.OccurredOn).IsRequired();
        builder.Property(e => e.Title).HasMaxLength(32);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.FromStage).HasConversion<string>().HasMaxLength(16);
        builder.Property(e => e.ToStage).HasConversion<string>().HasMaxLength(16);
        builder.Property(e => e.Text).HasMaxLength(StockistEvent.MaxTextLength);
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasIndex(e => new { e.StockistId, e.OccurredOn });
    }
}
