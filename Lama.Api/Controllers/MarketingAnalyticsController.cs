using Lama.Application.Common;
using Lama.Application.MarketingManagement.Commands;
using Lama.Domain.MarketingManagement.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Lama.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MarketingAnalyticsController : ControllerBase
{
    private readonly ICommandHandler<CreateMarketingAnalyticsCommand, Guid> _createAnalyticsHandler;
    private readonly IRepository<MarketingAnalytics> _analyticsRepository;

    public MarketingAnalyticsController(
        ICommandHandler<CreateMarketingAnalyticsCommand, Guid> createAnalyticsHandler,
        IRepository<MarketingAnalytics> analyticsRepository)
    {
        _createAnalyticsHandler = createAnalyticsHandler;
        _analyticsRepository = analyticsRepository;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateMarketingAnalytics([FromBody] CreateMarketingAnalyticsCommand command)
    {
        var analyticsId = await _createAnalyticsHandler.Handle(command);
        return CreatedAtAction(nameof(GetMarketingAnalytics), new { id = analyticsId }, analyticsId);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MarketingAnalytics>> GetMarketingAnalytics(Guid id)
    {
        var analytics = await _analyticsRepository.GetByIdAsync(id);

        if (analytics == null)
            return NotFound();

        return Ok(analytics);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MarketingAnalytics>>> GetAllMarketingAnalytics()
    {
        var analytics = await _analyticsRepository.GetAllAsync();
        return Ok(analytics);
    }
}
