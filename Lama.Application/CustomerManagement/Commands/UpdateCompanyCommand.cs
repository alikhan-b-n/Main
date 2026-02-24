using Lama.Application.Common;
using Lama.Domain.CustomerManagement.Entities;

namespace Lama.Application.CustomerManagement.Commands;

public record UpdateCompanyCommand(
    Guid Id,
    string Name,
    string? Industry = null,
    string? Website = null,
    string? Domain = null,
    Guid? ClientCategoryId = null,
    string? Email = null,
    string? PhoneNumber = null,
    string? Street = null,
    string? City = null,
    string? State = null,
    string? PostalCode = null,
    string? Country = null
) : ICommand;

public class UpdateCompanyCommandHandler : ICommandHandler<UpdateCompanyCommand>
{
    private readonly IRepository<Company> _companyRepository;

    public UpdateCompanyCommandHandler(IRepository<Company> companyRepository)
    {
        _companyRepository = companyRepository;
    }

    public async Task Handle(UpdateCompanyCommand command, CancellationToken cancellationToken = default)
    {
        var company = await _companyRepository.GetByIdAsync(command.Id, cancellationToken);
        if (company == null)
            throw new InvalidOperationException($"Company with ID {command.Id} not found");

        company.UpdateCompanyInfo(command.Name, command.Industry, command.Website, command.Domain);

        if (!string.IsNullOrWhiteSpace(command.Email) && !string.IsNullOrWhiteSpace(command.PhoneNumber))
        {
            company.SetContactInfo(command.Email, command.PhoneNumber);
        }

        if (!string.IsNullOrWhiteSpace(command.Street) && !string.IsNullOrWhiteSpace(command.City)
            && !string.IsNullOrWhiteSpace(command.State) && !string.IsNullOrWhiteSpace(command.PostalCode)
            && !string.IsNullOrWhiteSpace(command.Country))
        {
            company.SetAddress(command.Street, command.City, command.State, command.PostalCode, command.Country);
        }

        if (command.ClientCategoryId.HasValue)
        {
            company.AssignToCategory(command.ClientCategoryId.Value);
        }

        await _companyRepository.UpdateAsync(company, cancellationToken);
    }
}