using Lama.Application.Common;
using Lama.Domain.CustomerService.Entities;

namespace Lama.Application.CustomerService.Commands;

public record CreateSupportCaseCommand(
    string Title,
    string Description,
    Guid ContactId,
    CasePriority Priority,
    CaseType Type,
    Guid? AccountId = null
) : ICommand<Guid>;

public class CreateSupportCaseCommandHandler : ICommandHandler<CreateSupportCaseCommand, Guid>
{
    private readonly IRepository<SupportCase> _supportCaseRepository;

    public CreateSupportCaseCommandHandler(IRepository<SupportCase> supportCaseRepository)
    {
        _supportCaseRepository = supportCaseRepository;
    }

    public async Task<Guid> Handle(CreateSupportCaseCommand command, CancellationToken cancellationToken = default)
    {
        var supportCase = SupportCase.Create(
            command.Title,
            command.Description,
            command.ContactId,
            command.Priority,
            command.Type
        );

        if (command.AccountId.HasValue)
        {
            supportCase.AssignAccount(command.AccountId.Value);
        }

        await _supportCaseRepository.AddAsync(supportCase, cancellationToken);

        return supportCase.Id;
    }
}
