using Lama.Domain.Common;

namespace Lama.Domain.CustomerManagement.Entities;

public class OrganizationalRelationship : Entity
{
    public Guid SourceAccountId { get; private set; }
    public Account? SourceAccount { get; private set; }
    public Guid TargetAccountId { get; private set; }
    public Account? TargetAccount { get; private set; }
    public RelationshipType Type { get; private set; }
    public string? Description { get; private set; }

    private OrganizationalRelationship() { }

    private OrganizationalRelationship(Guid sourceAccountId, Guid targetAccountId, RelationshipType type)
    {
        SourceAccountId = sourceAccountId;
        TargetAccountId = targetAccountId;
        Type = type;
    }

    public static OrganizationalRelationship Create(Guid sourceAccountId, Guid targetAccountId, RelationshipType type, string? description = null)
    {
        if (sourceAccountId == Guid.Empty)
            throw new ArgumentException("Source account ID cannot be empty", nameof(sourceAccountId));
        if (targetAccountId == Guid.Empty)
            throw new ArgumentException("Target account ID cannot be empty", nameof(targetAccountId));
        if (sourceAccountId == targetAccountId)
            throw new ArgumentException("Source and target accounts cannot be the same");

        var relationship = new OrganizationalRelationship(sourceAccountId, targetAccountId, type)
        {
            Description = description
        };

        return relationship;
    }

    public void UpdateDescription(string description)
    {
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum RelationshipType
{
    ParentCompany,
    Subsidiary,
    Partner,
    Supplier,
    Distributor,
    Reseller
}
