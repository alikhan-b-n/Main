using Lama.Application.Common;

namespace Lama.Application.CustomerManagement.Queries;

public record GetAllAccountsQuery : IQuery<IEnumerable<AccountDto>>;
