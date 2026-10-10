namespace ChocolateToxicity.Clinical;

/// <summary>Units a constant rate infusion dose can be prescribed in.</summary>
public enum CriDoseUnit
{
    MicrogramsPerKgPerMinute,
    MicrogramsPerKgPerHour,
    MilligramsPerKgPerHour,
    MilligramsPerKgPerMinute,
}

/// <summary>Constant rate infusion (CRI) sums.</summary>
public static class ConstantRateInfusion
{
    /// <summary>Converts a CRI dose to mg/kg/hr.</summary>
    public static double ToMgPerKgPerHour(double dose, CriDoseUnit unit)
    {
        Guard.NotNegative(dose, nameof(dose));
        return unit switch
        {
            CriDoseUnit.MicrogramsPerKgPerMinute => dose * 60 / 1000,
            CriDoseUnit.MicrogramsPerKgPerHour => dose / 1000,
            CriDoseUnit.MilligramsPerKgPerHour => dose,
            CriDoseUnit.MilligramsPerKgPerMinute => dose * 60,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null),
        };
    }

    /// <summary>Drug delivered per hour, in mg/hr.</summary>
    public static double MgPerHour(double weightKg, double dose, CriDoseUnit unit)
    {
        Guard.Positive(weightKg, nameof(weightKg));
        return ToMgPerKgPerHour(dose, unit) * weightKg;
    }

    /// <summary>
    /// Undiluted drug run on its own (for example in a syringe driver): mL/hr = mg/hr / concentration (mg/mL).
    /// </summary>
    public static double UndilutedMlPerHour(double weightKg, double dose, CriDoseUnit unit, double concentrationMgPerMl)
    {
        Guard.Positive(concentrationMgPerMl, nameof(concentrationMgPerMl));
        return MgPerHour(weightKg, dose, unit) / concentrationMgPerMl;
    }

    /// <summary>
    /// Drug to add to a fluid bag or syringe so that running it at <paramref name="fluidRateMlPerHour"/> delivers the dose.
    /// mg to add = mg/hr x volume (mL) / rate (mL/hr); mL to add = mg / concentration (mg/mL).
    /// </summary>
    public static (double Mg, double Ml) AddToBag(
        double weightKg, double dose, CriDoseUnit unit, double concentrationMgPerMl, double bagVolumeMl, double fluidRateMlPerHour)
    {
        Guard.Positive(concentrationMgPerMl, nameof(concentrationMgPerMl));
        Guard.Positive(bagVolumeMl, nameof(bagVolumeMl));
        Guard.Positive(fluidRateMlPerHour, nameof(fluidRateMlPerHour));
        var mg = MgPerHour(weightKg, dose, unit) * bagVolumeMl / fluidRateMlPerHour;
        return (mg, mg / concentrationMgPerMl);
    }
}
