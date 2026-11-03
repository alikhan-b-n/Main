using Lama.Domain.OnlineSales.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lama.Infrastructure.Persistence.Configurations;

public class ShopifyConnectionConfiguration : IEntityTypeConfiguration<ShopifyConnection>
{
    public void Configure(EntityTypeBuilder<ShopifyConnection> builder)
    {
        builder.ToTable("ShopifyConnections");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Shop).IsRequired().HasMaxLength(ShopifyConnection.MaxShopLength);
        builder.Property(c => c.AccessToken).IsRequired().HasMaxLength(512);
        builder.Property(c => c.Scopes).IsRequired().HasMaxLength(512);
        builder.Property(c => c.RefreshToken).HasMaxLength(512);
        builder.Property(c => c.LastSyncError).HasMaxLength(ShopifyConnection.MaxErrorLength);
        builder.Property(c => c.ConnectedAt).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();
    }
}

public class OnlineOrderConfiguration : IEntityTypeConfiguration<OnlineOrder>
{
    public void Configure(EntityTypeBuilder<OnlineOrder> builder)
    {
        builder.ToTable("OnlineOrders");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.ShopifyId).IsRequired().HasMaxLength(OnlineOrder.MaxShopifyIdLength);
        builder.Property(o => o.Number).IsRequired().HasMaxLength(OnlineOrder.MaxNumberLength);
        builder.Property(o => o.PlacedOn).IsRequired();
        builder.Property(o => o.Amount).HasPrecision(18, 2);
        builder.Property(o => o.Currency).IsRequired().HasMaxLength(3);
        builder.Property(o => o.ChangedAt).IsRequired();
        builder.Property(o => o.CreatedAt).IsRequired();

        builder.Ignore(o => o.Counts);

        // Lines are rewritten in place on every update; the position keeps them in Shopify's order
        builder.OwnsMany(o => o.Lines, line =>
        {
            line.ToTable("OnlineOrderLines");
            line.WithOwner().HasForeignKey("OnlineOrderId");
            line.HasKey("OnlineOrderId", nameof(OnlineOrderLine.Position));
            line.Property(l => l.ProductTitle).IsRequired().HasMaxLength(OnlineOrder.MaxProductTitleLength);
            line.Property(l => l.Title).HasMaxLength(32);
            line.Property(l => l.Quantity).IsRequired();
        });
        builder.Navigation(o => o.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(o => o.ShopifyId).IsUnique();
        builder.HasIndex(o => o.PlacedOn);
    }
}
