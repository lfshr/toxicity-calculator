using System.Globalization;
using ChocolateToxicity.Core;

namespace ChocolateToxicity.ViewModels;

internal static class NumberInput
{
    // Accepts the device's decimal separator, and a full stop as well since many keypads only offer that.
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

    /// <summary>Validation message for the dog's weight, or null when it's valid.</summary>
    public static string? DogWeightError(double? weight) => weight switch
    {
        null => "Enter the dog's weight in kg.",
        <= 0 => "Weight must be more than 0 kg.",
        > 150 => "Check the weight: it should be in kg.",
        _ => null,
    };

    /// <summary>Up to one decimal place, dropping a trailing ".0", so "12 kg" and "52.3 mg/kg".</summary>
    public static string Format(double value) => value.ToString("0.#", CultureInfo.CurrentCulture);

    /// <summary>
    /// Like <see cref="Format"/>, but keeps enough decimal places for small values such as rat bait doses,
    /// so "0.0125 mg/kg" rather than "0 mg/kg".
    /// </summary>
    public static string FormatPrecise(double value) => DisplayedDose.Format(value);
}
