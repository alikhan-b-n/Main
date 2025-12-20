using Lama.Application.Common;
using Lama.Application.CustomerManagement.Commands;
using Lama.Application.CustomerManagement.Queries;
using Lama.Domain.CustomerManagement.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Lama.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IRepository<Account> _accountRepository;

    public AccountsController(
        IMediator mediator,
        IRepository<Account> accountRepository)
    {
        _mediator = mediator;
        _accountRepository = accountRepository;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateAccount([FromBody] CreateAccountCommand command)
    {
        var accountId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetAccount), new { id = accountId }, accountId);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AccountDto>> GetAccount(Guid id)
    {
        var account = await _accountRepository.GetByIdAsync(id);

        if (account == null)
            return NotFound();

        var dto = new AccountDto(
            account.Id,
            account.Name,
            account.Industry,
            account.Website,
            account.Type.ToString(),
            account.CreatedAt
        );

        return Ok(dto);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AccountDto>>> GetAllAccounts()
    {
        var accounts = await _accountRepository.GetAllAsync();

        var dtos = accounts.Select(a => new AccountDto(
            a.Id,
            a.Name,
            a.Industry,
            a.Website,
            a.Type.ToString(),
            a.CreatedAt
        ));

        return Ok(dtos);
    }
}
