using Lama.Domain.Common;
using Lama.Domain.CustomerManagement.ValueObjects;

namespace Lama.Domain.CustomerManagement.Entities;

public class Contact : AggregateRoot
{
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public Email Email { get; private set; }
    public PhoneNumber? PhoneNumber { get; private set; }
    public string? JobTitle { get; private set; }
    public Guid? CompanyId { get; private set; }
    public Company? Company { get; private set; }
    public Guid? OwnerId { get; private set; }
    public DateTime LastActivityAt { get; private set; }
    public string? LifecycleStage { get; private set; }
    public decimal TotalRevenueContribution { get; private set; }

    private Contact() { }

    private Contact(string firstName, string lastName, Email email)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        LifecycleStage = "Lead";
        TotalRevenueContribution = 0;
        LastActivityAt = DateTime.UtcNow;
    }

    public static Contact Create(string firstName, string lastName, string email)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be empty", nameof(lastName));

        return new Contact(firstName, lastName, Email.Create(email));
    }

    public void UpdateContactInfo(string firstName, string lastName, string? jobTitle)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be empty", nameof(lastName));

        FirstName = firstName;
        LastName = lastName;
        JobTitle = jobTitle;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPhoneNumber(string phoneNumber)
    {
        PhoneNumber = PhoneNumber.Create(phoneNumber);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignToCompany(Guid companyId)
    {
        CompanyId = companyId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignToOwner(Guid ownerId)
    {
        OwnerId = ownerId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateLifecycleStage(string stage)
    {
        // Stages: Lead, MQL, SQL, Opportunity, Customer, Evangelist
        LifecycleStage = stage;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordRevenueContribution(decimal amount)
    {
        TotalRevenueContribution += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateLastActivity()
    {
        LastActivityAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public string GetFullName() => $"{FirstName} {LastName}";
}
