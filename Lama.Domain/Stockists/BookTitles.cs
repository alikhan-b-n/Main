namespace Lama.Domain.Stockists;

/// <summary>
/// The Noah's Adventures picture books a store can stock. Stock, deliveries and filters
/// refer to a title by its code, so the codes must stay stable; the UI shows the names.
/// </summary>
public static class BookTitles
{
    public static readonly IReadOnlyList<string> Codes = new[]
    {
        "cork",
        "galway",
        "kerry",
        "berlin",
        "waterford",
        "tipperary"
    };

    public static bool IsKnown(string? code) => code != null && Codes.Contains(code);

    /// <summary>Lower-cases and trims a code, or throws when it is not one of the titles.</summary>
    public static string Normalize(string? code)
    {
        var normalized = code?.Trim().ToLowerInvariant();
        if (!IsKnown(normalized))
            throw new ArgumentException($"Unknown book title: {code}. Valid values: {string.Join(", ", Codes)}", nameof(code));
        return normalized!;
    }
}
