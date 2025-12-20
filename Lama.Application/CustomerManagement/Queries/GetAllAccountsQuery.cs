using Lama.Application.Common;
using Lama.Domain.CustomerManagement.Entities;

namespace Lama.Application.CustomerManagement.Queries;

public record GetAllAccountsQuery : IQuery<IEnumerable<AccountDto>>;

public class GetAllAccountsQueryHandler : IQueryHandler<GetAllAccountsQuery, IEnumerable<AccountDto>>
{
    private readonly IRepository<Account> _accountRepository;

    public GetAllAccountsQueryHandler(IRepository<Account> accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<IEnumerable<AccountDto>> Handle(GetAllAccountsQuery query, CancellationToken cancellationToken)
    {
        var accounts = await _accountRepository.GetAllAsync(cancellationToken);

        return accounts.Select(a => new AccountDto(
            a.Id,
            a.Name,
            a.Industry,
            a.Website,
            a.Type.ToString(),
            a.CreatedAt
        ));
    }
}
