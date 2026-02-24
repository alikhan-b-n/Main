using System;
using System.Threading.Tasks;
using Lama.Integrations.AI.Commands;
using Lama.Integrations.AI.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Lama.Api.Controllers;

[ApiController]
[Route("crm/ai/activities")]
public class AiActivitiesController : ControllerBase
{
    private readonly IMediator _mediator;

    public AiActivitiesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // POST /crm/ai/activities/{id}/summarize
    [HttpPost("{id:guid}/summarize")]
    public async Task<ActionResult<ActivitySummaryDto>> SummarizeActivity(Guid id)
    {
        var command = new SummarizeActivityCommand(id);
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}

