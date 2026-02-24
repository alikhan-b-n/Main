using Lama.Application.CustomerManagement.Commands;
using Lama.Application.CustomerManagement.Queries;
using Lama.Application.Common;
using Lama.Domain.CustomerManagement.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Lama.Api.Controllers;

[ApiController]
[Route("crm/objects/client_categories")]
public class ClientCategoriesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IRepository<ClientCategory> _repository;

    public ClientCategoriesController(IMediator mediator, IRepository<ClientCategory> repository)
    {
        _mediator = mediator;
        _repository = repository;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateClientCategory([FromBody] CreateClientCategoryCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetClientCategory), new { id }, id);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ClientCategory>> GetClientCategory(Guid id)
    {
        var category = await _repository.GetByIdAsync(id);
        if (category == null) return NotFound();
        return Ok(category);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClientCategoryDto>>> GetAll()
    {
        var query = new GetAllClientCategoriesQuery();
        var categories = await _mediator.Send(query);
        return Ok(categories);
    }
}
