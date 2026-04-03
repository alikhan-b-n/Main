using Lama.Application.Common;
using Lama.Domain.CustomerManagement.Entities;

namespace Lama.Application.CustomerManagement.Commands;

public record CreateCompanyCommand(
    string Name,
    string? Industry = null,
    string? Website = null,
    string? Domain = null
) : ICommand<Guid>;

public class CreateCompanyCommandHandler : ICommandHandler<CreateCompanyCommand, Guid>
{
    private readonly IRepository<Company> _companyRepository;

    public CreateCompanyCommandHandler(IRepository<Company> companyRepository)
    {
        _companyRepository = companyRepository;
    }

    public async Task<Guid> Handle(CreateCompanyCommand command, CancellationToken cancellationToken = default)
    {
        var company = Company.Create(command.Name, command.Industry);

        if (!string.IsNullOrWhiteSpace(command.Website) || !string.IsNullOrWhiteSpace(command.Domain))
        {
            company.UpdateCompanyInfo(command.Name, command.Industry, command.Website, command.Domain);
        }

        await _companyRepository.AddAsync(company, cancellationToken);

        return company.Id;
    }
}
