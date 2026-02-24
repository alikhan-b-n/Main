using Lama.Application.Common;
using Lama.Domain.CustomerManagement.Entities;

namespace Lama.Application.CustomerManagement.Commands;

public record DeleteCompanyCommand(Guid Id) : ICommand;

public class DeleteCompanyCommandHandler : ICommandHandler<DeleteCompanyCommand>
{
    private readonly IRepository<Company> _companyRepository;

    public DeleteCompanyCommandHandler(IRepository<Company> companyRepository)
    {
        _companyRepository = companyRepository;
    }

    public async Task Handle(DeleteCompanyCommand command, CancellationToken cancellationToken = default)
    {
        var company = await _companyRepository.GetByIdAsync(command.Id, cancellationToken);
        if (company == null)
            throw new InvalidOperationException($"Company with ID {command.Id} not found");

        await _companyRepository.DeleteAsync(company, cancellationToken);
    }
}