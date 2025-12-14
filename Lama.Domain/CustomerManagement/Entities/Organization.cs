using Lama.Domain.Common;
using Lama.Domain.CustomerManagement.ValueObjects;

namespace Lama.Domain.CustomerManagement.Entities;

public class Organization : AggregateRoot
{
    public string Name { get; private set; }
    public string? LegalName { get; private set; }
    public string? TaxId { get; private set; }
    public OrganizationType Type { get; private set; }
    public OrganizationSize Size { get; private set; }
    public string? Industry { get; private set; }
    public Address? HeadquartersAddress { get; private set; }
    public Email? PrimaryEmail { get; private set; }
    public PhoneNumber? PrimaryPhone { get; private set; }
    public string? Website { get; private set; }
    public DateTime? FoundedDate { get; private set; }
    public int? EmployeeCount { get; private set; }
    public decimal? AnnualRevenue { get; private set; }
    public string? StockSymbol { get; private set; }
    public Guid? ParentOrganizationId { get; private set; }
    public Organization? ParentOrganization { get; private set; }

    private readonly List<Organization> _subsidiaries = new();
    public IReadOnlyCollection<Organization> Subsidiaries => _subsidiaries.AsReadOnly();

    private readonly List<Account> _accounts = new();
    public IReadOnlyCollection<Account> Accounts => _accounts.AsReadOnly();

    private readonly List<string> _locations = new();
    public IReadOnlyCollection<string> Locations => _locations.AsReadOnly();

    private readonly Dictionary<string, string> _customFields = new();
    public IReadOnlyDictionary<string, string> CustomFields => _customFields;

    private Organization() { }

    private Organization(string name, OrganizationType type, OrganizationSize size)
    {
        Name = name;
        Type = type;
        Size = size;
    }

    public static Organization Create(string name, OrganizationType type, OrganizationSize size)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Organization name cannot be empty", nameof(name));

        return new Organization(name, type, size);
    }

    public void UpdateBasicInfo(string name, string? legalName, string? industry, string? website)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Organization name cannot be empty", nameof(name));

        Name = name;
        LegalName = legalName;
        Industry = industry;
        Website = website;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateCompanyInfo(DateTime? foundedDate, int? employeeCount, decimal? annualRevenue, string? stockSymbol)
    {
        FoundedDate = foundedDate;
        EmployeeCount = employeeCount;
        AnnualRevenue = annualRevenue;
        StockSymbol = stockSymbol;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetHeadquarters(string street, string city, string state, string postalCode, string country)
    {
        HeadquartersAddress = Address.Create(street, city, state, postalCode, country);
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetContactInfo(string email, string phoneNumber)
    {
        PrimaryEmail = Email.Create(email);
        PrimaryPhone = PhoneNumber.Create(phoneNumber);
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetTaxId(string taxId)
    {
        if (string.IsNullOrWhiteSpace(taxId))
            throw new ArgumentException("Tax ID cannot be empty", nameof(taxId));

        TaxId = taxId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetParentOrganization(Guid parentOrganizationId)
    {
        if (parentOrganizationId == Id)
            throw new InvalidOperationException("Organization cannot be its own parent");

        ParentOrganizationId = parentOrganizationId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddSubsidiary(Organization subsidiary)
    {
        if (_subsidiaries.Any(s => s.Id == subsidiary.Id))
            throw new InvalidOperationException("Subsidiary already exists");

        _subsidiaries.Add(subsidiary);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddLocation(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location cannot be empty", nameof(location));

        if (_locations.Contains(location))
            throw new InvalidOperationException("Location already exists");

        _locations.Add(location);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddCustomField(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Custom field key cannot be empty", nameof(key));

        _customFields[key] = value;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateSize(OrganizationSize newSize)
    {
        Size = newSize;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum OrganizationType
{
    Corporation,
    Partnership,
    SoleProprietorship,
    LimitedLiabilityCompany,
    NonProfit,
    Government,
    Educational,
    Other
}

public enum OrganizationSize
{
    Startup,           // 1-10 employees
    Small,             // 11-50 employees
    Medium,            // 51-200 employees
    MidMarket,         // 201-1000 employees
    Enterprise,        // 1001-10000 employees
    LargeEnterprise    // 10000+ employees
}
