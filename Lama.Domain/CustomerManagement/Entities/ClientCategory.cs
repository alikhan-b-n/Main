using Lama.Domain.Common;

namespace Lama.Domain.CustomerManagement.Entities;

public class ClientCategory : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int PriorityLevel { get; private set; }
    public string? DiscountPolicy { get; private set; }

    private ClientCategory() { }

    private ClientCategory(string name, int priorityLevel)
    {
        Name = name;
        PriorityLevel = priorityLevel;
    }

    public static ClientCategory Create(string name, int priorityLevel, string? description = null, string? discountPolicy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Client category name cannot be empty", nameof(name));
        if (priorityLevel < 1 || priorityLevel > 10)
            throw new ArgumentException("Priority level must be between 1 and 10", nameof(priorityLevel));

        return new ClientCategory(name, priorityLevel)
        {
            Description = description,
            DiscountPolicy = discountPolicy
        };
    }

    public void UpdateCategory(string name, int priorityLevel, string? description, string? discountPolicy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Client category name cannot be empty", nameof(name));
        if (priorityLevel < 1 || priorityLevel > 10)
            throw new ArgumentException("Priority level must be between 1 and 10", nameof(priorityLevel));

        Name = name;
        PriorityLevel = priorityLevel;
        Description = description;
        DiscountPolicy = discountPolicy;
    }
}
