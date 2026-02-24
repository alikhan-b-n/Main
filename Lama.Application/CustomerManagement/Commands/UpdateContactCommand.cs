using Lama.Application.Common;
using Lama.Domain.CustomerManagement.Entities;

namespace Lama.Application.CustomerManagement.Commands;

public record UpdateContactCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string? JobTitle = null,
    string? PhoneNumber = null,
    Guid? CompanyId = null,
    Guid? OwnerId = null,
    string? LifecycleStage = null
) : ICommand;

public class UpdateContactCommandHandler : ICommandHandler<UpdateContactCommand>
{
    private readonly IRepository<Contact> _contactRepository;

    public UpdateContactCommandHandler(IRepository<Contact> contactRepository)
    {
        _contactRepository = contactRepository;
    }

    public async Task Handle(UpdateContactCommand command, CancellationToken cancellationToken = default)
    {
        var contact = await _contactRepository.GetByIdAsync(command.Id, cancellationToken);
        if (contact == null)
            throw new InvalidOperationException($"Contact with ID {command.Id} not found");

        contact.UpdateContactInfo(command.FirstName, command.LastName, command.JobTitle);

        if (!string.IsNullOrWhiteSpace(command.PhoneNumber))
        {
            contact.SetPhoneNumber(command.PhoneNumber);
        }

        if (command.CompanyId.HasValue)
        {
            contact.AssignToCompany(command.CompanyId.Value);
        }

        if (command.OwnerId.HasValue)
        {
            contact.AssignToOwner(command.OwnerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(command.LifecycleStage))
        {
            contact.UpdateLifecycleStage(command.LifecycleStage);
        }

        await _contactRepository.UpdateAsync(contact, cancellationToken);
    }
}