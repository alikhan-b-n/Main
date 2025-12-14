using Lama.Application.Common;

namespace Lama.Application.CustomerManagement.Queries;

public record GetAccountByIdQuery(Guid AccountId) : IQuery<AccountDto?>;

public record AccountDto(
    Guid Id,
    string Name,
    string? Industry,
    string? Website,
    string Type,
    DateTime CreatedAt
);
