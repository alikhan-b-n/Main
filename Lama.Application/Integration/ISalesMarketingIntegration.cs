namespace Lama.Application.Integration;

/// <summary>
/// Integration service for Sales Management -> Marketing Management
/// Sales insights inform marketing campaign strategies
/// </summary>
public interface ISalesMarketingIntegration
{
    /// <summary>
    /// Get successful sales data to inform marketing campaigns
    /// </summary>
    Task<SalesInsightsDto> GetSalesInsightsAsync(DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get top performing products/services for marketing focus
    /// </summary>
    Task<IEnumerable<ProductPerformanceDto>> GetTopPerformingProductsAsync(int topN, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get conversion rates by source to optimize marketing spend
    /// </summary>
    Task<IEnumerable<ConversionRateDto>> GetConversionRatesBySourceAsync(CancellationToken cancellationToken = default);
}

public record SalesInsightsDto(
    decimal TotalRevenue,
    int TotalOpportunities,
    int WonOpportunities,
    decimal AverageDealSize,
    decimal WinRate
);

public record ProductPerformanceDto(string ProductName, decimal Revenue, int UnitsSold);
public record ConversionRateDto(string Source, int Leads, int Conversions, decimal Rate);
