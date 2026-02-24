using Lama.Application.Common;
using Lama.Domain.SalesManagement.Entities;

namespace Lama.Application.SalesManagement.Commands;

public record DeleteDealCommand(Guid Id) : ICommand;

public class DeleteDealCommandHandler : ICommandHandler<DeleteDealCommand>
{
    private readonly IRepository<Deal> _dealRepository;

    public DeleteDealCommandHandler(IRepository<Deal> dealRepository)
    {
        _dealRepository = dealRepository;
    }

    public async Task Handle(DeleteDealCommand command, CancellationToken cancellationToken = default)
    {
        var deal = await _dealRepository.GetByIdAsync(command.Id, cancellationToken);
        if (deal == null)
            throw new InvalidOperationException($"Deal with ID {command.Id} not found");

        await _dealRepository.DeleteAsync(deal, cancellationToken);
    }
}