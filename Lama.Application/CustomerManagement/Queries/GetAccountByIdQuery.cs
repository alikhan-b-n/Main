using Lama.Application.Common;
using Lama.Domain.CustomerManagement.Entities;

namespace Lama.Application.CustomerManagement.Queries;

public record GetAccountByIdQuery(Guid AccountId) : IQuery<AccountDto?>;

public class GetAccountByIdQueryHandler : IQueryHandler<GetAccountByIdQuery, AccountDto?>
{
    private readonly IRepository<Account> _accountRepository;

    public GetAccountByIdQueryHandler(IRepository<Account> accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<AccountDto?> Handle(GetAccountByIdQuery query, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(query.AccountId, cancellationToken);

        if (account == null)
            return null;

        return new AccountDto(
            account.Id,
            account.Name,
            account.Industry,
            account.Website,
            account.Type.ToString(),
            account.CreatedAt
        );
    }
}

public record AccountDto(
    Guid Id,
    string Name,
    string? Industry,
    string? Website,
    string Type,
    DateTime CreatedAt
);
