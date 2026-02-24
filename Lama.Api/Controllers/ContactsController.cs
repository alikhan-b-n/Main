using Lama.Application.Common;
using Lama.Application.CustomerManagement.Commands;
using Lama.Application.CustomerManagement.Queries;
using Lama.Domain.CustomerManagement.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Lama.Api.Controllers;

[ApiController]
[Route("crm/objects/contacts")]
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
    public async Task<ActionResult<IEnumerable<ContactDto>>> GetAllContacts()
    {
        var query = new GetAllContactsQuery();
        var contacts = await _mediator.Send(query);

        return Ok(contacts);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateContact(Guid id, [FromBody] UpdateContactCommand command)
    {
        if (id != command.Id)
            return BadRequest(new { message = "ID mismatch" });

        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteContact(Guid id)
    {
        var command = new DeleteContactCommand(id);
        await _mediator.Send(command);
        return NoContent();
    }
}
