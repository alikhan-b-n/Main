using Lama.Domain.Common;
using Lama.Domain.CustomerManagement.ValueObjects;

namespace Lama.Domain.AccessControl.Entities;

/// <summary>
/// Someone who signs in to the CRM. Accounts are created by an administrator —
/// there is no self-service registration.
/// </summary>
public class CrmUser : AggregateRoot
{
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 128;

    public Email Email { get; private set; }
    public string FullName { get; private set; }
    /// <summary>Hash only — the plain password never leaves the request.</summary>
    public string PasswordHash { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    public bool IsAdmin => Role == UserRole.Admin;

    private CrmUser()
    {
        Email = null!;
        FullName = null!;
        PasswordHash = null!;
    }

    public static CrmUser Create(string email, string fullName, string passwordHash, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty", nameof(passwordHash));

        return new CrmUser
        {
            Email = Email.Create(email.Trim()),
            FullName = NormalizeName(fullName),
            PasswordHash = passwordHash,
            Role = role,
            IsActive = true
        };
    }

    public void Rename(string fullName)
    {
        FullName = NormalizeName(fullName);
        Touch();
    }

    public void ChangeRole(UserRole role)
    {
        Role = role;
        Touch();
    }

    public void ChangePassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty", nameof(passwordHash));

        PasswordHash = passwordHash;
        Touch();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        Touch();
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        Touch();
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;

    private static string NormalizeName(string fullName)
    {
        var cleaned = (fullName ?? string.Empty).Trim();
        if (cleaned.Length < 2)
            throw new ArgumentException("Name is too short", nameof(fullName));
        if (cleaned.Length > 100)
            throw new ArgumentException("Name must not exceed 100 characters", nameof(fullName));
        return cleaned;
    }
}

public enum UserRole
{
    /// <summary>Works with leads and contacts.</summary>
    Manager,
    /// <summary>Everything a manager can do, plus managing CRM users.</summary>
    Admin
}
