using Lama.Application.Common;
using Lama.Domain.CustomerManagement.Entities;

namespace Lama.Application.CustomerManagement.Commands;

public record CreateOrganizationCommand(
    string Name,
    OrganizationType Type,
    OrganizationSize Size,
    string? LegalName = null,
    string? Industry = null,
    string? Website = null
) : ICommand<Guid>;

public class CreateOrganizationCommandHandler : ICommandHandler<CreateOrganizationCommand, Guid>
{
    private readonly IRepository<Organization> _organizationRepository;

    public CreateOrganizationCommandHandler(IRepository<Organization> organizationRepository)
    {
        _organizationRepository = organizationRepository;
    }

    public async Task<Guid> Handle(CreateOrganizationCommand command, CancellationToken cancellationToken = default)
    {
        var organization = Organization.Create(command.Name, command.Type, command.Size);

        if (!string.IsNullOrWhiteSpace(command.LegalName) ||
            !string.IsNullOrWhiteSpace(command.Industry) ||
            !string.IsNullOrWhiteSpace(command.Website))
        {
            organization.UpdateBasicInfo(command.Name, command.LegalName, command.Industry, command.Website);
        }

        await _organizationRepository.AddAsync(organization, cancellationToken);

        return organization.Id;
    }
}
