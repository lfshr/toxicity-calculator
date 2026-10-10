namespace ChocolateToxicity.Clinical;

public static class UnitConversion
{
    public const double PoundsPerKg = 2.20462262;

    public static double KgToLb(double kg) => kg * PoundsPerKg;

    public static double LbToKg(double lb) => lb / PoundsPerKg;

    public static double CelsiusToFahrenheit(double c) => c * 9 / 5 + 32;

    public static double FahrenheitToCelsius(double f) => (f - 32) * 5 / 9;

    /// <summary>A w/v percentage is grams per 100 mL, so 1% = 10 mg/mL.</summary>
    public static double PercentToMgPerMl(double percent) => percent * 10;

    public static double MgPerMlToPercent(double mgPerMl) => mgPerMl / 10;
}
