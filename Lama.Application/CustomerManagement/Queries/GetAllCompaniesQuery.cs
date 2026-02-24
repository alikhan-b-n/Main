using Lama.Application.Common;
using Lama.Domain.CustomerManagement.Entities;

namespace Lama.Application.CustomerManagement.Queries;

public record GetAllCompaniesQuery : IQuery<IEnumerable<CompanyDto>>;

public class GetAllCompaniesQueryHandler : IQueryHandler<GetAllCompaniesQuery, IEnumerable<CompanyDto>>
{
    private readonly IRepository<Company> _companyRepository;

    public GetAllCompaniesQueryHandler(IRepository<Company> companyRepository)
    {
        _companyRepository = companyRepository;
    }

    public async Task<IEnumerable<CompanyDto>> Handle(GetAllCompaniesQuery query, CancellationToken cancellationToken)
    {
        var companies = await _companyRepository.GetAllAsync(cancellationToken);

        return companies.Select(c => new CompanyDto(
            c.Id,
            c.Name,
            c.Domain,
            c.Industry,
            c.Website,
            c.ClientCategoryId,
            c.TotalSpent,
            c.CreatedAt,
            c.LastActivityAt
        ));
    }
}

public record CompanyDto(
    Guid Id,
    string Name,
    string? Domain,
    string? Industry,
    string? Website,
    Guid? ClientCategoryId,
    decimal TotalSpent,
    DateTime CreatedAt,
    DateTime LastActivityAt
);
