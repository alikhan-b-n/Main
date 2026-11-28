namespace Lama.Domain.LeadManagement;

/// <summary>
/// Works out which country a lead is from by their phone number. Leads type numbers
/// in every format (with or without "+", Kazakh "8 7xx", local "0700…"), so the rules
/// below were tuned against the real leads rather than a full numbering plan.
/// It is computed on read, never stored, so changing a rule here re-labels old leads too.
/// </summary>
public static class PhoneCountry
{
    /// <summary>The key used for leads whose phone is missing or not recognised.</summary>
    public const string Unknown = "unknown";

    // ITU calling code → ISO 3166-1 alpha-2. Matched longest first, so "998" wins over "99…".
    private static readonly Dictionary<string, string> CallingCodes = new()
    {
        // Central Asia, Caucasus and the rest of the former USSR
        ["998"] = "UZ", ["996"] = "KG", ["992"] = "TJ", ["993"] = "TM", ["994"] = "AZ",
        ["995"] = "GE", ["374"] = "AM", ["375"] = "BY", ["380"] = "UA", ["373"] = "MD",
        ["370"] = "LT", ["371"] = "LV", ["372"] = "EE",
        // Europe
        ["30"] = "GR", ["31"] = "NL", ["32"] = "BE", ["33"] = "FR", ["34"] = "ES",
        ["351"] = "PT", ["353"] = "IE", ["358"] = "FI", ["359"] = "BG", ["36"] = "HU",
        ["381"] = "RS", ["385"] = "HR", ["39"] = "IT", ["40"] = "RO", ["41"] = "CH",
        ["420"] = "CZ", ["421"] = "SK", ["43"] = "AT", ["44"] = "GB", ["45"] = "DK",
        ["46"] = "SE", ["47"] = "NO", ["48"] = "PL", ["49"] = "DE",
        // Middle East and Asia
        ["90"] = "TR", ["971"] = "AE", ["972"] = "IL", ["974"] = "QA", ["966"] = "SA",
        ["98"] = "IR", ["91"] = "IN", ["92"] = "PK", ["93"] = "AF", ["86"] = "CN",
        ["82"] = "KR", ["81"] = "JP", ["852"] = "HK", ["84"] = "VN", ["66"] = "TH",
        ["60"] = "MY", ["62"] = "ID", ["63"] = "PH", ["65"] = "SG", ["976"] = "MN",
        // Elsewhere
        ["1"] = "US", ["20"] = "EG", ["27"] = "ZA", ["61"] = "AU", ["64"] = "NZ",
        ["55"] = "BR", ["52"] = "MX",
    };

    private static readonly int MaxCodeLength = CallingCodes.Keys.Max(k => k.Length);

    /// <summary>ISO alpha-2 code of the phone's country, or null when it cannot be told.</summary>
    public static string? FromPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return null;

        var trimmed = phone.Trim();
        var hasPlus = trimmed.StartsWith('+');
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
            return null;

        // "00" is the international prefix: 00996… is +996….
        if (!hasPlus && digits.StartsWith("00"))
        {
            digits = digits[2..];
            hasPlus = true;
        }

        if (!hasPlus)
        {
            // Local formats with a leading 0: Kyrgyz 0700…/0704…/0552…, Armenian 033….
            if (digits.StartsWith('0'))
            {
                if (digits.StartsWith("033"))
                    return "AM";
                if (digits.Length == 10 && (digits[1] == '7' || digits[1] == '5'))
                    return "KG";
                return null;
            }

            // Kazakh/Russian domestic trunk prefix: 8 7xx… is +7 7xx….
            if (digits.Length == 11 && digits[0] == '8')
                digits = "7" + digits[1..];
            // 89… with a digit missing: a Russian mobile typed short.
            else if (digits.Length == 10 && digits.StartsWith("89"))
                return "RU";
        }

        if (digits[0] == '7')
            return FromPlusSeven(digits);

        for (var length = Math.Min(MaxCodeLength, digits.Length); length > 0; length--)
        {
            if (CallingCodes.TryGetValue(digits[..length], out var country))
                return country;
        }

        return null;
    }

    /// <summary>The country, or <see cref="Unknown"/>: the key used by filters and stats.</summary>
    public static string KeyFromPhone(string? phone) => FromPhone(phone) ?? Unknown;

    // +7 is shared: Kazakhstan has +7 6xx and +7 7xx, Russia the rest.
    private static string? FromPlusSeven(string digits)
    {
        if (digits.Length < 2)
            return null;

        // A 10-digit +79… is a Russian mobile with one digit missing.
        if (digits.Length == 10 && digits[1] == '9')
            return "RU";

        return digits[1] switch
        {
            '6' or '7' => "KZ",
            '3' or '4' or '8' or '9' => "RU",
            _ => null,
        };
    }
}
