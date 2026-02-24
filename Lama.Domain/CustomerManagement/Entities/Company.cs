using Lama.Domain.Common;
using Lama.Domain.CustomerManagement.ValueObjects;

namespace Lama.Domain.CustomerManagement.Entities;

public class Company : AggregateRoot
{
    public string Name { get; private set; }
    public string? Domain { get; private set; }
    public string? Industry { get; private set; }
    public Address? Address { get; private set; }
    public Email? Email { get; private set; }
    public PhoneNumber? PhoneNumber { get; private set; }
    public string? Website { get; private set; }
    public Guid? ClientCategoryId { get; private set; }
    public decimal TotalSpent { get; private set; }
    public DateTime LastActivityAt { get; private set; }

    private readonly List<Contact> _contacts = new();
    public IReadOnlyCollection<Contact> Contacts => _contacts.AsReadOnly();

    private Company() { }

    private Company(string name, string? industry)
    {
        Name = name;
        Industry = industry;
        TotalSpent = 0;
        LastActivityAt = DateTime.UtcNow;
    }

    public static Company Create(string name, string? industry = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Company name cannot be empty", nameof(name));

        return new Company(name, industry);
    }

    public void UpdateCompanyInfo(string name, string? industry, string? website, string? domain)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Company name cannot be empty", nameof(name));

        Name = name;
        Industry = industry;
        Website = website;
        Domain = domain;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAddress(string street, string city, string state, string postalCode, string country)
    {
        Address = Address.Create(street, city, state, postalCode, country);
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetContactInfo(string email, string phoneNumber)
    {
        Email = Email.Create(email);
        PhoneNumber = PhoneNumber.Create(phoneNumber);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddContact(Contact contact)
    {
        if (_contacts.Any(c => c.Id == contact.Id))
            throw new InvalidOperationException("Contact already exists in this company");

        _contacts.Add(contact);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignToCategory(Guid clientCategoryId)
    {
        ClientCategoryId = clientCategoryId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordSpending(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));

        TotalSpent += amount;
        LastActivityAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateLastActivity()
    {
        LastActivityAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
