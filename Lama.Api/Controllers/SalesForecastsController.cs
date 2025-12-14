using Lama.Application.Common;
using Lama.Application.SalesManagement.Commands;
using Lama.Domain.SalesManagement.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Lama.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalesForecastsController : ControllerBase
{
    private readonly ICommandHandler<CreateSalesForecastCommand, Guid> _createForecastHandler;
    private readonly IRepository<SalesForecast> _forecastRepository;

    public SalesForecastsController(
        ICommandHandler<CreateSalesForecastCommand, Guid> createForecastHandler,
        IRepository<SalesForecast> forecastRepository)
    {
        _createForecastHandler = createForecastHandler;
        _forecastRepository = forecastRepository;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateSalesForecast([FromBody] CreateSalesForecastCommand command)
    {
        var forecastId = await _createForecastHandler.Handle(command);
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
