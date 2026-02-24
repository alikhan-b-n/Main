using Lama.Application.Common;
using Lama.Domain.SalesManagement.Entities;

namespace Lama.Application.SalesManagement.Commands;

public record CreateDealCommand(
    string Name,
    Guid CompanyId,
    decimal Amount,
    DateTime ExpectedCloseDate,
    Guid PipelineId,
    Guid StageId,
    string Currency = "USD",
    Guid? ContactId = null,
    string? Description = null,
    Guid? OwnerId = null
) : ICommand<Guid>;

public class CreateDealCommandHandler : ICommandHandler<CreateDealCommand, Guid>
{
    private readonly IRepository<Deal> _dealRepository;

    public CreateDealCommandHandler(IRepository<Deal> dealRepository)
    {
        _dealRepository = dealRepository;
    }

    public async Task<Guid> Handle(CreateDealCommand command, CancellationToken cancellationToken = default)
    {
        var deal = Deal.Create(
            command.Name,
            command.CompanyId,
            command.Amount,
            command.ExpectedCloseDate,
            command.PipelineId,
            command.StageId,
            command.Currency
        );

        if (!string.IsNullOrWhiteSpace(command.Description))
        {
            deal.UpdateDealInfo(
                command.Name,
                command.Description,
                command.Amount,
                command.ExpectedCloseDate
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

        await _dealRepository.AddAsync(deal, cancellationToken);

        return deal.Id;
    }
}
