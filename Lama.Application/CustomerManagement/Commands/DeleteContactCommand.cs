using Lama.Application.Common;
using Lama.Domain.CustomerManagement.Entities;

namespace Lama.Application.CustomerManagement.Commands;

public record DeleteContactCommand(Guid Id) : ICommand;

public class DeleteContactCommandHandler : ICommandHandler<DeleteContactCommand>
{
    private readonly IRepository<Contact> _contactRepository;

    public DeleteContactCommandHandler(IRepository<Contact> contactRepository)
    {
        _contactRepository = contactRepository;
    }

    public async Task Handle(DeleteContactCommand command, CancellationToken cancellationToken = default)
    {
        var contact = await _contactRepository.GetByIdAsync(command.Id, cancellationToken);
        if (contact == null)
            throw new InvalidOperationException($"Contact with ID {command.Id} not found");

        await _contactRepository.DeleteAsync(contact, cancellationToken);
    }
}