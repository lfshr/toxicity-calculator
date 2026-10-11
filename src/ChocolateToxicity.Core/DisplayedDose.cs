using System.Globalization;

namespace ChocolateToxicity.Core;

/// <summary>
/// The precision doses are shown at, which is also the precision they are compared with thresholds at. A dose shown
/// as "20 mg/kg" is then always banded as 20 mg/kg: neither floating-point error (19.999999999999996) nor display
/// rounding (19.96 shown as "20") can put it below a 20 mg/kg threshold while the screen says 20.
/// </summary>
public static class DisplayedDose
{
    /// <summary>
    /// The number format for a dose: one decimal place from 10, two from 1, three from 0.01 and five below that, so
    /// small doses such as rat bait (0.0125 mg/kg) keep their significant figures.
    /// </summary>
    public static string FormatString(double value) => Math.Abs(value) switch
    {
        >= 10 => "0.#",
        >= 1 => "0.##",
        >= 0.01 => "0.###",
        _ => "0.#####",
    };

    /// <summary>Formats a dose as results show it, such as "52.3" or "0.0125".</summary>
    public static string Format(double value, IFormatProvider? provider = null) =>
        value == 0 ? "0" : value.ToString(FormatString(value), provider ?? CultureInfo.CurrentCulture);

    /// <summary>The dose as shown on screen, as a number.</summary>
    public static double Round(double value) =>
        double.Parse(Format(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
}
