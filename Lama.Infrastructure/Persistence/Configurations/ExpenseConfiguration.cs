using Lama.Domain.Expenses.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lama.Infrastructure.Persistence.Configurations;

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("Expenses");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Category).IsRequired().HasConversion<string>().HasMaxLength(16);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.StartsOn).IsRequired();
        builder.Property(e => e.Note).HasMaxLength(Expense.MaxNoteLength);
        builder.Property(e => e.CreatedAt).IsRequired();

        // Shipping keeps the store it went to; deleting the store leaves the cost, which was still paid
        builder.HasOne<Lama.Domain.Stockists.Entities.Stockist>()
            .WithMany()
            .HasForeignKey(e => e.StockistId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.StartsOn);
    }
}
