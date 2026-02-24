using Lama.Application.Common;
using Lama.Domain.CustomerManagement.Entities;

namespace Lama.Application.CustomerManagement.Commands;

public record CreateContactCommand(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber = null,
    string? JobTitle = null,
    Guid? CompanyId = null,
    Guid? OwnerId = null
) : ICommand<Guid>;

public class CreateContactCommandHandler : ICommandHandler<CreateContactCommand, Guid>
{
    private readonly IRepository<Contact> _contactRepository;

    public CreateContactCommandHandler(IRepository<Contact> contactRepository)
    {
        _contactRepository = contactRepository;
    }

    public async Task<Guid> Handle(CreateContactCommand command, CancellationToken cancellationToken = default)
    {
        var contact = Contact.Create(command.FirstName, command.LastName, command.Email);

        if (!string.IsNullOrWhiteSpace(command.PhoneNumber))
        {
            contact.SetPhoneNumber(command.PhoneNumber);
        }

        if (!string.IsNullOrWhiteSpace(command.JobTitle))
        {
            contact.UpdateContactInfo(command.FirstName, command.LastName, command.JobTitle);
        }

        if (command.CompanyId.HasValue)
        {
            contact.AssignToCompany(command.CompanyId.Value);
        }

        if (command.OwnerId.HasValue)
        {
            contact.AssignToOwner(command.OwnerId.Value);
        }

        await _contactRepository.AddAsync(contact, cancellationToken);

        return contact.Id;
    }
}
