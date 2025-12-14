using Lama.Application.Common;
using Lama.Domain.SalesManagement.Entities;

namespace Lama.Application.SalesManagement.Commands;

public record CreateOpportunityCommand(
    string Name,
    Guid AccountId,
    decimal ExpectedRevenue,
    DateTime ExpectedCloseDate,
    string Currency = "USD",
    Guid? ContactId = null,
    string? Description = null
) : ICommand<Guid>;

public class CreateOpportunityCommandHandler : ICommandHandler<CreateOpportunityCommand, Guid>
{
    private readonly IRepository<Opportunity> _opportunityRepository;

    public CreateOpportunityCommandHandler(IRepository<Opportunity> opportunityRepository)
    {
        _opportunityRepository = opportunityRepository;
    }

    public async Task<Guid> Handle(CreateOpportunityCommand command, CancellationToken cancellationToken = default)
    {
        var opportunity = Opportunity.Create(
            command.Name,
            command.AccountId,
            command.ExpectedRevenue,
            command.ExpectedCloseDate,
            command.Currency
        );

        if (!string.IsNullOrWhiteSpace(command.Description))
        {
            opportunity.UpdateOpportunityInfo(
                command.Name,
                command.Description,
                command.ExpectedRevenue,
                command.ExpectedCloseDate
            );
        }

        if (command.ContactId.HasValue)
        {
            opportunity.AssignContact(command.ContactId.Value);
        }

        await _opportunityRepository.AddAsync(opportunity, cancellationToken);

        return opportunity.Id;
    }
}
