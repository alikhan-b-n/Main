using Lama.Application.LeadManagement;
using Lama.Application.LeadManagement.Queries;
using Lama.Domain.LeadManagement;
using Lama.Domain.LeadManagement.Entities;

namespace Lama.Tests.LeadManagement;

/// <summary>The formats below are the ones real leads typed into the bot, TikTok and the site form.</summary>
public class PhoneCountryTests
{
    [Theory]
    // +7 6xx / +7 7xx is Kazakhstan, the rest of +7 is Russia.
    [InlineData("+7 701 234 56 78", "KZ")]
    [InlineData("+77071234567", "KZ")]
    [InlineData("+7 (747) 123-45-67", "KZ")]
    [InlineData("+76001234567", "KZ")]
    [InlineData("+7 916 123 45 67", "RU")]
    [InlineData("+73431234567", "RU")]
    [InlineData("+74951234567", "RU")]
    [InlineData("+78121234567", "RU")]
    // Without "+": 7… is still +7.
    [InlineData("77011234567", "KZ")]
    [InlineData("79161234567", "RU")]
    // Domestic trunk prefix 8: 8 7xx is Kazakhstan, 8 9xx Russia.
    [InlineData("87011234567", "KZ")]
    [InlineData("8 (777) 123 45 67", "KZ")]
    [InlineData("89161234567", "RU")]
    // A Russian mobile with a digit missing.
    [InlineData("+7916123456", "RU")]
    [InlineData("8916123456", "RU")]
    // Local formats with a leading 0.
    [InlineData("0700123456", "KG")]
    [InlineData("0704 12 34 56", "KG")]
    [InlineData("0552123456", "KG")]
    [InlineData("033123456", "AM")]
    // Longest calling code wins.
    [InlineData("+998901234567", "UZ")]
    [InlineData("998901234567", "UZ")]
    [InlineData("+996700123456", "KG")]
    [InlineData("+992901234567", "TJ")]
    [InlineData("+994501234567", "AZ")]
    [InlineData("+375291234567", "BY")]
    [InlineData("+380501234567", "UA")]
    [InlineData("+37369123456", "MD")]
    [InlineData("+37061234567", "LT")]
    [InlineData("+905321234567", "TR")]
    [InlineData("+821012345678", "KR")]
    [InlineData("+48501234567", "PL")]
    [InlineData("+393331234567", "IT")]
    [InlineData("+36201234567", "HU")]
    [InlineData("+33612345678", "FR")]
    [InlineData("+41791234567", "CH")]
    [InlineData("00996700123456", "KG")]
    public void RecognisesCountry(string phone, string expected)
    {
        Assert.Equal(expected, PhoneCountry.FromPhone(phone));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("нет")]
    [InlineData("0123456789")]
    [InlineData("+7")]
    public void UnrecognisedPhone_IsUnknown(string? phone)
    {
        Assert.Null(PhoneCountry.FromPhone(phone));
        Assert.Equal(PhoneCountry.Unknown, PhoneCountry.KeyFromPhone(phone));
    }

    [Fact]
    public void Stats_BreakDownByPhoneCountry_WithUnknownBucket()
    {
        var now = new DateTime(2026, 11, 28, 12, 0, 0, DateTimeKind.Utc);
        LeadStatsRow Row(string? phone) =>
            new(LeadStatus.New, LeadTemperature.Warm, "telegram", null, null, Array.Empty<string>(), now, phone);

        var stats = LeadStatsCalculator.Calculate(new[]
        {
            Row("+77011234567"), Row("87471234567"), Row("+79161234567"),
            Row("0700123456"), Row(null),
        }, now);

        Assert.Equal(
            new[] { ("KZ", 2), ("KG", 1), ("RU", 1), ("unknown", 1) },
            stats.ByPhoneCountry.Select(c => (c.Key, c.Count)));
    }
}
