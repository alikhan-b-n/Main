using Lama.Domain.CustomerManagement.Entities;
using Lama.Domain.CustomerManagement.ValueObjects;
using Lama.Domain.CustomerService.Entities;
using Lama.Domain.MarketingManagement.Entities;
using Lama.Domain.SalesManagement.Entities;
using Lama.Domain.SalesManagement.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Lama.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // Customer Management
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationalRelationship> OrganizationalRelationships => Set<OrganizationalRelationship>();

    // Sales Management
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<SalesActivity> SalesActivities => Set<SalesActivity>();
    public DbSet<SalesForecast> SalesForecasts => Set<SalesForecast>();
    public DbSet<SalesPipeline> SalesPipelines => Set<SalesPipeline>();

    // Marketing Management
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignMetric> CampaignMetrics => Set<CampaignMetric>();
    public DbSet<CustomerSegment> CustomerSegments => Set<CustomerSegment>();
    public DbSet<MarketingAnalytics> MarketingAnalytics => Set<MarketingAnalytics>();

    // Customer Service
    public DbSet<SupportCase> SupportCases => Set<SupportCase>();
    public DbSet<CaseInteraction> CaseInteractions => Set<CaseInteraction>();
    public DbSet<ServiceWorkflow> ServiceWorkflows => Set<ServiceWorkflow>();

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
        // Email value object
        modelBuilder.Entity<Account>()
            .OwnsOne(a => a.Email, email =>
            {
                email.Property(e => e.Value).HasColumnName("Email").HasMaxLength(255);
            });

        modelBuilder.Entity<Contact>()
            .OwnsOne(c => c.Email, email =>
            {
                email.Property(e => e.Value).HasColumnName("Email").HasMaxLength(255);
            });

        // PhoneNumber value object
        modelBuilder.Entity<Account>()
            .OwnsOne(a => a.PhoneNumber, phone =>
            {
                phone.Property(p => p.Value).HasColumnName("PhoneNumber").HasMaxLength(50);
            });

        modelBuilder.Entity<Contact>()
            .OwnsOne(c => c.PhoneNumber, phone =>
            {
                phone.Property(p => p.Value).HasColumnName("PhoneNumber").HasMaxLength(50);
            });

        // Address value object (only Account has Address)
        modelBuilder.Entity<Account>()
            .OwnsOne(a => a.Address, address =>
            {
                address.Property(ad => ad.Street).HasColumnName("Street").HasMaxLength(255);
                address.Property(ad => ad.City).HasColumnName("City").HasMaxLength(100);
                address.Property(ad => ad.State).HasColumnName("State").HasMaxLength(100);
                address.Property(ad => ad.PostalCode).HasColumnName("PostalCode").HasMaxLength(20);
                address.Property(ad => ad.Country).HasColumnName("Country").HasMaxLength(100);
            });

        // Money value object (used in Opportunity)
        modelBuilder.Entity<Opportunity>()
            .OwnsOne(o => o.ExpectedRevenue, money =>
            {
                money.Property(m => m.Amount).HasColumnName("ExpectedRevenueAmount").HasPrecision(18, 2);
                money.Property(m => m.Currency).HasColumnName("ExpectedRevenueCurrency").HasMaxLength(3);
            });

        // Organization value objects
        modelBuilder.Entity<Organization>()
            .OwnsOne(o => o.HeadquartersAddress, address =>
            {
                address.Property(ad => ad.Street).HasColumnName("HeadquartersStreet").HasMaxLength(255);
                address.Property(ad => ad.City).HasColumnName("HeadquartersCity").HasMaxLength(100);
                address.Property(ad => ad.State).HasColumnName("HeadquartersState").HasMaxLength(100);
                address.Property(ad => ad.PostalCode).HasColumnName("HeadquartersPostalCode").HasMaxLength(20);
                address.Property(ad => ad.Country).HasColumnName("HeadquartersCountry").HasMaxLength(100);
            });

        modelBuilder.Entity<Organization>()
            .OwnsOne(o => o.PrimaryEmail, email =>
            {
                email.Property(e => e.Value).HasColumnName("PrimaryEmail").HasMaxLength(255);
            });

        modelBuilder.Entity<Organization>()
            .OwnsOne(o => o.PrimaryPhone, phone =>
            {
                phone.Property(p => p.Value).HasColumnName("PrimaryPhone").HasMaxLength(50);
            });

        // SalesForecast value objects (Money)
        modelBuilder.Entity<SalesForecast>()
            .OwnsOne(sf => sf.Quota, money =>
            {
                money.Property(m => m.Amount).HasColumnName("QuotaAmount").HasPrecision(18, 2);
                money.Property(m => m.Currency).HasColumnName("QuotaCurrency").HasMaxLength(3);
            });

        modelBuilder.Entity<SalesForecast>()
            .OwnsOne(sf => sf.ProjectedRevenue, money =>
            {
                money.Property(m => m.Amount).HasColumnName("ProjectedRevenueAmount").HasPrecision(18, 2);
                money.Property(m => m.Currency).HasColumnName("ProjectedRevenueCurrency").HasMaxLength(3);
            });

        modelBuilder.Entity<SalesForecast>()
            .OwnsOne(sf => sf.ActualRevenue, money =>
            {
                money.Property(m => m.Amount).HasColumnName("ActualRevenueAmount").HasPrecision(18, 2);
                money.Property(m => m.Currency).HasColumnName("ActualRevenueCurrency").HasMaxLength(3);
            });
    }
}