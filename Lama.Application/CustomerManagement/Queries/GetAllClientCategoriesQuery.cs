// ...existing code...
using Lama.Application.Common;
using Lama.Domain.CustomerManagement.Entities;

namespace Lama.Application.CustomerManagement.Queries;

public record GetAllClientCategoriesQuery : IQuery<IEnumerable<ClientCategoryDto>>;

public class GetAllClientCategoriesQueryHandler : IQueryHandler<GetAllClientCategoriesQuery, IEnumerable<ClientCategoryDto>>
{
    private readonly IRepository<ClientCategory> _repository;

    public GetAllClientCategoriesQueryHandler(IRepository<ClientCategory> repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ClientCategoryDto>> Handle(GetAllClientCategoriesQuery query, CancellationToken cancellationToken)
    {
        var categories = await _repository.GetAllAsync(cancellationToken);

        return categories.Select(c => new ClientCategoryDto(
            c.Id,
            c.Name,
            c.Description,
            c.PriorityLevel,
            c.DiscountPolicy
        ));
    }
}

public record ClientCategoryDto(
    Guid Id,
    string Name,
    string? Description,
    int PriorityLevel,
    string? DiscountPolicy
);
// ...existing code...
