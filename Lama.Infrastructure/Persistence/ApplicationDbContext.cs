using Lama.Domain.CustomerManagement.Entities;
using Lama.Domain.CustomerService.Entities;
using Lama.Domain.SalesManagement.Entities;
using Lama.Domain.ActivityManagement.Entities;
using Lama.Domain.UserManagement.Entities;
using Lama.Domain.PipelineManagement.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lama.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // Customer Management (Core CRM)
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<ClientCategory> ClientCategories => Set<ClientCategory>();

    // Sales Management (Core CRM)
    public DbSet<Deal> Deals => Set<Deal>();

    // Customer Service (Core CRM)
    public DbSet<Ticket> Tickets => Set<Ticket>();

    // Activity & User Management
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Employee> Employees => Set<Employee>();

    // Pipeline Management
    public DbSet<Pipeline> Pipelines => Set<Pipeline>();
    public DbSet<Stage> Stages => Set<Stage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Configure value objects
        ConfigureValueObjects(modelBuilder);
    }

    private void ConfigureValueObjects(ModelBuilder modelBuilder)
    {
        // Email value object for Company
        modelBuilder.Entity<Company>()
            .OwnsOne(c => c.Email, email =>
            {
                email.Property(e => e.Value).HasColumnName("Email").HasMaxLength(255);
            });

        // PhoneNumber value object for Company
        modelBuilder.Entity<Company>()
            .OwnsOne(c => c.PhoneNumber, phone =>
            {
                phone.Property(p => p.Value).HasColumnName("PhoneNumber").HasMaxLength(50);
            });

        // Address value object for Company
        modelBuilder.Entity<Company>()
            .OwnsOne(c => c.Address, address =>
            {
                address.Property(ad => ad.Street).HasColumnName("Street").HasMaxLength(255);
                address.Property(ad => ad.City).HasColumnName("City").HasMaxLength(100);
                address.Property(ad => ad.State).HasColumnName("State").HasMaxLength(100);
                address.Property(ad => ad.PostalCode).HasColumnName("PostalCode").HasMaxLength(20);
                address.Property(ad => ad.Country).HasColumnName("Country").HasMaxLength(100);
            });

        // Email value object for Contact
        modelBuilder.Entity<Contact>()
            .OwnsOne(c => c.Email, email =>
            {
                email.Property(e => e.Value).HasColumnName("Email").HasMaxLength(255);
            });

        // PhoneNumber value object for Contact
        modelBuilder.Entity<Contact>()
            .OwnsOne(c => c.PhoneNumber, phone =>
            {
                phone.Property(p => p.Value).HasColumnName("PhoneNumber").HasMaxLength(50);
            });

        // Money value object for Deal
        modelBuilder.Entity<Deal>()
            .OwnsOne(d => d.Amount, money =>
            {
                money.Property(m => m.Amount).HasColumnName("Amount").HasPrecision(18, 2);
                money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3);
            });
    }
}
