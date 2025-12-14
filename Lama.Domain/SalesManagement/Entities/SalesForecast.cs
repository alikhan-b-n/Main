using Lama.Domain.Common;
using Lama.Domain.SalesManagement.ValueObjects;

namespace Lama.Domain.SalesManagement.Entities;

public class SalesForecast : AggregateRoot
{
    public string Name { get; private set; }
    public ForecastPeriod Period { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public Money Quota { get; private set; }
    public Money ProjectedRevenue { get; private set; }
    public Money ActualRevenue { get; private set; }
    public ForecastStatus Status { get; private set; }
    public decimal ConfidenceLevel { get; private set; }

    private readonly List<ForecastLineItem> _lineItems = new();
    public IReadOnlyCollection<ForecastLineItem> LineItems => _lineItems.AsReadOnly();

    private SalesForecast() { }

    private SalesForecast(string name, ForecastPeriod period, DateTime startDate, DateTime endDate, Money quota)
    {
        Name = name;
        Period = period;
        StartDate = startDate;
        EndDate = endDate;
        Quota = quota;
        ProjectedRevenue = Money.Create(0, quota.Currency);
        ActualRevenue = Money.Create(0, quota.Currency);
        Status = ForecastStatus.Draft;
        ConfidenceLevel = 0;
    }

    public static SalesForecast Create(string name, ForecastPeriod period, DateTime startDate, DateTime endDate, decimal quota, string currency = "USD")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Forecast name cannot be empty", nameof(name));
        if (startDate >= endDate)
            throw new ArgumentException("Start date must be before end date");
        if (quota < 0)
            throw new ArgumentException("Quota cannot be negative", nameof(quota));

        return new SalesForecast(name, period, startDate, endDate, Money.Create(quota, currency));
    }

    public void AddLineItem(ForecastLineItem lineItem)
    {
        if (_lineItems.Any(li => li.OpportunityId == lineItem.OpportunityId))
            throw new InvalidOperationException("Opportunity already exists in forecast");

        _lineItems.Add(lineItem);
        RecalculateProjectedRevenue();
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveLineItem(Guid opportunityId)
    {
        var lineItem = _lineItems.FirstOrDefault(li => li.OpportunityId == opportunityId);
        if (lineItem == null)
            throw new InvalidOperationException("Line item not found");

        _lineItems.Remove(lineItem);
        RecalculateProjectedRevenue();
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordActualRevenue(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));

        ActualRevenue = Money.Create(amount, Quota.Currency);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Publish()
    {
        if (Status != ForecastStatus.Draft)
            throw new InvalidOperationException("Only draft forecasts can be published");

        Status = ForecastStatus.Published;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Close()
    {
        if (Status != ForecastStatus.Published)
            throw new InvalidOperationException("Only published forecasts can be closed");

        Status = ForecastStatus.Closed;
        UpdatedAt = DateTime.UtcNow;
    }

    public decimal GetAchievementPercentage()
    {
        if (Quota.Amount == 0)
            return 0;

        return (ActualRevenue.Amount / Quota.Amount) * 100;
    }

    public decimal GetProjectedAchievementPercentage()
    {
        if (Quota.Amount == 0)
            return 0;

        return (ProjectedRevenue.Amount / Quota.Amount) * 100;
    }

    private void RecalculateProjectedRevenue()
    {
        var total = _lineItems.Sum(li => li.WeightedAmount);
        ProjectedRevenue = Money.Create(total, Quota.Currency);

        if (_lineItems.Any())
        {
            ConfidenceLevel = (decimal)_lineItems.Average(li => li.Probability);
        }
    }
}

public class ForecastLineItem
{
    public Guid OpportunityId { get; set; }
    public string OpportunityName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Probability { get; set; }
    public decimal WeightedAmount => Amount * Probability / 100m;
    public DateTime ExpectedCloseDate { get; set; }
}

public enum ForecastPeriod
{
    Monthly,
    Quarterly,
    Yearly
}

public enum ForecastStatus
{
    Draft,
    Published,
    Closed
}
