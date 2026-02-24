using Lama.Application.Common;
using Lama.Domain.SalesManagement.Entities;

namespace Lama.Application.SalesManagement.Commands;

public record UpdateDealCommand(
    Guid Id,
    string Name,
    string? Description = null,
    decimal? Amount = null,
    DateTime? ExpectedCloseDate = null,
    Guid? ContactId = null,
    Guid? OwnerId = null,
    Guid? StageId = null,
    int? Probability = null
) : ICommand;

public class UpdateDealCommandHandler : ICommandHandler<UpdateDealCommand>
{
    private readonly IRepository<Deal> _dealRepository;

    public UpdateDealCommandHandler(IRepository<Deal> dealRepository)
    {
        _dealRepository = dealRepository;
    }

    public async Task Handle(UpdateDealCommand command, CancellationToken cancellationToken = default)
    {
        var deal = await _dealRepository.GetByIdAsync(command.Id, cancellationToken);
        if (deal == null)
            throw new InvalidOperationException($"Deal with ID {command.Id} not found");

        if (command.Amount.HasValue || command.ExpectedCloseDate.HasValue)
        {
            deal.UpdateDealInfo(
                command.Name,
                command.Description,
                command.Amount ?? deal.Amount.Amount,
                command.ExpectedCloseDate ?? deal.ExpectedCloseDate
            );
        }

        if (command.ContactId.HasValue)
        {
            deal.AssignContact(command.ContactId.Value);
        }

        if (command.OwnerId.HasValue)
        {
            deal.AssignOwner(command.OwnerId.Value);
        }

        if (command.StageId.HasValue && command.Probability.HasValue)
        {
            deal.MoveToStage(command.StageId.Value, command.Probability.Value);
        }
        else if (command.Probability.HasValue)
        {
            deal.UpdateProbability(command.Probability.Value);
        }

        await _dealRepository.UpdateAsync(deal, cancellationToken);
    }
}