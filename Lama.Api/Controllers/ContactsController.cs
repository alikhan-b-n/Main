using Lama.Application.Common;
using Lama.Application.CustomerManagement.Commands;
using Lama.Domain.CustomerManagement.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Lama.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IRepository<Contact> _contactRepository;

    public ContactsController(
        IMediator mediator,
        IRepository<Contact> contactRepository)
    {
        _mediator = mediator;
        _contactRepository = contactRepository;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateContact([FromBody] CreateContactCommand command)
    {
        var contactId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetContact), new { id = contactId }, contactId);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Contact>> GetContact(Guid id)
    {
        var contact = await _contactRepository.GetByIdAsync(id);

        if (contact == null)
            return NotFound();

        return Ok(contact);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Contact>>> GetAllContacts()
    {
        var contacts = await _contactRepository.GetAllAsync();
        return Ok(contacts);
    }
}
