using Lama.Application.Common;
using Lama.Domain.SalesManagement.Entities;

namespace Lama.Application.SalesManagement.Commands;

public record CreateSalesForecastCommand(
    string Name,
    ForecastPeriod Period,
    DateTime StartDate,
    DateTime EndDate,
    decimal Quota,
    string Currency = "USD"
) : ICommand<Guid>;

public class CreateSalesForecastCommandHandler : ICommandHandler<CreateSalesForecastCommand, Guid>
{
    private readonly IRepository<SalesForecast> _forecastRepository;

    public CreateSalesForecastCommandHandler(IRepository<SalesForecast> forecastRepository)
    {
        _forecastRepository = forecastRepository;
    }

    public async Task<Guid> Handle(CreateSalesForecastCommand command, CancellationToken cancellationToken = default)
    {
        var forecast = SalesForecast.Create(
            command.Name,
            command.Period,
            command.StartDate,
            command.EndDate,
            command.Quota,
            command.Currency
        );

        await _forecastRepository.AddAsync(forecast, cancellationToken);

        return forecast.Id;
    }
}
