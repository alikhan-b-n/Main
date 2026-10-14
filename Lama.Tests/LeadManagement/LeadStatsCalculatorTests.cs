using Lama.Application.LeadManagement;
using Lama.Application.LeadManagement.Queries;
using Lama.Domain.LeadManagement.Entities;

namespace Lama.Tests.LeadManagement;

public class LeadStatsCalculatorTests
{
    private static readonly DateTime Now = new(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc);

    private static LeadStatsRow Row(
        LeadStatus status,
        LeadTemperature temperature = LeadTemperature.Warm,
        int daysAgo = 1,
        string source = "instagram",
        string? budget = "1000_3000",
        string? degree = "master",
        params string[] countries) =>
        new(status, temperature, source, budget, degree, countries, Now.AddDays(-daysAgo));

    [Fact]
    public void EmptyData_GivesZeroesAndFullBuckets()
    {
        var stats = LeadStatsCalculator.Calculate(Array.Empty<LeadStatsRow>(), Now);

        Assert.Equal(0, stats.Total);
        Assert.Equal(0, stats.ConversionRate);
        Assert.Equal(Enum.GetNames<LeadStatus>(), stats.ByStatus.Select(s => s.Key));
        Assert.Equal(new[] { "Hot", "Warm", "Cold" }, stats.ByTemperature.Select(t => t.Key));
        Assert.All(stats.ByStatus, s => Assert.Equal(0, s.Count));
        Assert.Empty(stats.BySource);
    }

    [Fact]
    public void IgnoredLeadsAreNeitherOpenNorHeldAgainstTheConversion()
    {
        var rows = new[]
        {
            Row(LeadStatus.New, LeadTemperature.Hot),
            Row(LeadStatus.ContractSigned, LeadTemperature.Hot, daysAgo: 10),
            Row(LeadStatus.Lost, LeadTemperature.Cold, daysAgo: 12),
            // Spam and wrong numbers: they happened, but they are not work in progress
            Row(LeadStatus.Ignored, LeadTemperature.Cold, daysAgo: 2),
            Row(LeadStatus.Ignored, LeadTemperature.Hot, daysAgo: 3),
        };

        var stats = LeadStatsCalculator.Calculate(rows, Now);

        Assert.Equal(5, stats.Total);
        // Only the new one is open — the two ignored ones are not
        Assert.Equal(1, stats.Open);
        Assert.Equal(1, stats.HotOpen);
        // One signed out of one signed plus one lost; the ignored pair stays out of it
        Assert.Equal(50, stats.ConversionRate);
        Assert.Equal(2, stats.ByStatus.Single(s => s.Key == "Ignored").Count);
    }

    [Fact]
    public void CountsFunnelWeeksAndConversion()
    {
        var rows = new[]
        {
            Row(LeadStatus.New, LeadTemperature.Hot, daysAgo: 1, countries: new[] { "de", "pl" }),
            Row(LeadStatus.Contacted, LeadTemperature.Hot, daysAgo: 3, source: "tiktok", countries: new[] { "de" }),
            Row(LeadStatus.ContractSigned, LeadTemperature.Hot, daysAgo: 10),
            Row(LeadStatus.Lost, LeadTemperature.Cold, daysAgo: 12, budget: null, degree: null),
            Row(LeadStatus.Lost, LeadTemperature.Cold, daysAgo: 30),
        };

        var stats = LeadStatsCalculator.Calculate(rows, Now);

        Assert.Equal(5, stats.Total);
        Assert.Equal(2, stats.Open);
        Assert.Equal(2, stats.NewThisWeek);
        Assert.Equal(2, stats.NewPreviousWeek);
        Assert.Equal(2, stats.HotOpen);
        Assert.Equal(1, stats.ContractsSigned);
        Assert.Equal(33, stats.ConversionRate);

        Assert.Equal(2, stats.ByStatus.Single(s => s.Key == "Lost").Count);
        Assert.Equal(new CountItem("instagram", 4), stats.BySource[0]);
        Assert.Equal(new CountItem("de", 2), stats.ByCountry[0]);
        Assert.Equal(4, stats.ByBudget.Sum(b => b.Count));
        Assert.Equal(4, stats.ByDegree.Sum(d => d.Count));
    }
}
