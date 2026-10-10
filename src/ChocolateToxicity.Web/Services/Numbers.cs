using System.Globalization;

namespace ChocolateToxicity.Web.Services;

/// <summary>Reading and showing numbers on the calculator pages.</summary>
public static class Numbers
{
    // Accepts the browser's decimal separator, and a full stop as well.
    public static double? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        const NumberStyles style = NumberStyles.Float;
        if (double.TryParse(text, style, CultureInfo.CurrentCulture, out var value) ||
            double.TryParse(text, style, CultureInfo.InvariantCulture, out value))
        {
            return double.IsFinite(value) ? value : null;
        }

        return null;
    }

    /// <summary>Shows a value with sensible precision: more decimal places for small doses and volumes.</summary>
    public static string Format(double value) => Math.Abs(value) switch
    {
        0 => "0",
        >= 100 => value.ToString("0", CultureInfo.CurrentCulture),
        >= 10 => value.ToString("0.#", CultureInfo.CurrentCulture),
        >= 1 => value.ToString("0.##", CultureInfo.CurrentCulture),
        >= 0.01 => value.ToString("0.###", CultureInfo.CurrentCulture),
        _ => value.ToString("0.#####", CultureInfo.CurrentCulture),
    };
}
