using Lama.Application.Common;
using Lama.Domain.CustomerManagement.Entities;

namespace Lama.Application.CustomerManagement.Commands;

public record CreateClientCategoryCommand(
    string Name,
    int PriorityLevel,
    string? Description = null,
    string? DiscountPolicy = null
) : ICommand<Guid>;

public class CreateClientCategoryCommandHandler : ICommandHandler<CreateClientCategoryCommand, Guid>
{
    private readonly IRepository<ClientCategory> _repository;

    public CreateClientCategoryCommandHandler(IRepository<ClientCategory> repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(CreateClientCategoryCommand command, CancellationToken cancellationToken = default)
    {
        var category = ClientCategory.Create(command.Name, command.PriorityLevel, command.Description, command.DiscountPolicy);
        await _repository.AddAsync(category, cancellationToken);
        return category.Id;
    }
}
