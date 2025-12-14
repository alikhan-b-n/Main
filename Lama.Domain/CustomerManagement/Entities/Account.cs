using Lama.Domain.Common;
using Lama.Domain.CustomerManagement.ValueObjects;

namespace Lama.Domain.CustomerManagement.Entities;

public class Account : AggregateRoot
{
    public string Name { get; private set; }
    public string? Industry { get; private set; }
    public Address? Address { get; private set; }
    public Email? Email { get; private set; }
    public PhoneNumber? PhoneNumber { get; private set; }
    public string? Website { get; private set; }
    public AccountType Type { get; private set; }

    private readonly List<Contact> _contacts = new();
    public IReadOnlyCollection<Contact> Contacts => _contacts.AsReadOnly();

    private readonly List<OrganizationalRelationship> _relationships = new();
    public IReadOnlyCollection<OrganizationalRelationship> Relationships => _relationships.AsReadOnly();

    private Account() { }

    private Account(string name, AccountType type)
    {
        Name = name;
        Type = type;
    }

    public static Account Create(string name, AccountType type)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Account name cannot be empty", nameof(name));

        return new Account(name, type);
    }

    public void UpdateAccountInfo(string name, string? industry, string? website)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Account name cannot be empty", nameof(name));

        Name = name;
        Industry = industry;
        Website = website;
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
            throw new InvalidOperationException("Contact already exists in this account");

        _contacts.Add(contact);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddRelationship(OrganizationalRelationship relationship)
    {
        if (_relationships.Any(r => r.Id == relationship.Id))
            throw new InvalidOperationException("Relationship already exists");

        _relationships.Add(relationship);
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum AccountType
{
    Prospect,
    Customer,
    Partner,
    Competitor
}
