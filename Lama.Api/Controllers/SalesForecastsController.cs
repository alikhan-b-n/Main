using Lama.Application.Common;
using Lama.Application.SalesManagement.Commands;
using Lama.Domain.SalesManagement.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Lama.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalesForecastsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IRepository<SalesForecast> _forecastRepository;

    public SalesForecastsController(
        IMediator mediator,
        IRepository<SalesForecast> forecastRepository)
    {
        _mediator = mediator;
        _forecastRepository = forecastRepository;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateSalesForecast([FromBody] CreateSalesForecastCommand command)
    {
        var forecastId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetSalesForecast), new { id = forecastId }, forecastId);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SalesForecast>> GetSalesForecast(Guid id)
    {
        var forecast = await _forecastRepository.GetByIdAsync(id);

        if (forecast == null)
            return NotFound();

        return Ok(forecast);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SalesForecast>>> GetAllSalesForecasts()
    {
        var forecasts = await _forecastRepository.GetAllAsync();
        return Ok(forecasts);
    }
}
