using Lama.Domain.Common;

namespace Lama.Domain.UserManagement.Entities;

public class Employee : AggregateRoot
{
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string? Role { get; private set; }
    public Guid? TeamId { get; private set; }
    public bool IsActive { get; private set; }

    private Employee() { }

    private Employee(string name, string email)
    {
        Name = name;
        Email = email;
        IsActive = true;
    }

    public static Employee Create(string name, string email, string? role = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Employee name cannot be empty", nameof(name));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Employee email cannot be empty", nameof(email));

        var employee = new Employee(name, email);
        if (!string.IsNullOrWhiteSpace(role))
            employee.Role = role;

        return employee;
    }

    public void UpdateEmployeeInfo(string name, string email, string? role)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Employee name cannot be empty", nameof(name));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Employee email cannot be empty", nameof(email));

        Name = name;
        Email = email;
        Role = role;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignToTeam(Guid teamId)
    {
        TeamId = teamId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateRole(string role)
    {
        Role = role;
        UpdatedAt = DateTime.UtcNow;
    }
}
