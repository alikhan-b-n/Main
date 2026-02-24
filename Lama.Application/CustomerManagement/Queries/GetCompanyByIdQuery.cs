using Lama.Application.Common;
using Lama.Domain.CustomerManagement.Entities;

namespace Lama.Application.CustomerManagement.Queries;

public record GetCompanyByIdQuery(Guid CompanyId) : IQuery<CompanyDto?>;

public class GetCompanyByIdQueryHandler : IQueryHandler<GetCompanyByIdQuery, CompanyDto?>
{
    private readonly IRepository<Company> _companyRepository;

    public GetCompanyByIdQueryHandler(IRepository<Company> companyRepository)
    {
        _companyRepository = companyRepository;
    }

    public async Task<CompanyDto?> Handle(GetCompanyByIdQuery query, CancellationToken cancellationToken)
    {
        var company = await _companyRepository.GetByIdAsync(query.CompanyId, cancellationToken);

        if (company == null)
            return null;

        return new CompanyDto(
            company.Id,
            company.Name,
            company.Domain,
            company.Industry,
            company.Website,
            company.ClientCategoryId,
            company.TotalSpent,
            company.CreatedAt,
            company.LastActivityAt
        );
    }
}
