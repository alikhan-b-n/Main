using Lama.Application.Common;
using Lama.Domain.CustomerManagement.Entities;

namespace Lama.Application.CustomerManagement.Commands;

public record CreateAccountCommand(
    string Name,
    AccountType Type,
    string? Industry = null,
    string? Website = null
) : ICommand<Guid>;

public class CreateAccountCommandHandler : ICommandHandler<CreateAccountCommand, Guid>
{
    private readonly IRepository<Account> _accountRepository;

    public CreateAccountCommandHandler(IRepository<Account> accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<Guid> Handle(CreateAccountCommand command, CancellationToken cancellationToken = default)
    {
        var account = Account.Create(command.Name, command.Type);

        if (!string.IsNullOrWhiteSpace(command.Industry) || !string.IsNullOrWhiteSpace(command.Website))
        {
            account.UpdateAccountInfo(command.Name, command.Industry, command.Website);
        }

        await _accountRepository.AddAsync(account, cancellationToken);

        return account.Id;
    }
}
