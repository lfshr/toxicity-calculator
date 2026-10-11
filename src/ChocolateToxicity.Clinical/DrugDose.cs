namespace ChocolateToxicity.Clinical;

/// <summary>A weight-based dose worked out as an amount of drug and, when the strength is known, a volume or tablet count.</summary>
/// <param name="TotalMg">Total drug to give, in mg (or units, for drugs dosed in units).</param>
/// <param name="VolumeMl">Volume of the stated concentration, in mL, when a concentration was given.</param>
/// <param name="Tablets">Number of tablets of the stated strength, when a tablet strength was given.</param>
public sealed record DrugDoseResult(double TotalMg, double? VolumeMl, double? Tablets);

/// <summary>Converts a mg/kg dose to mg, mL and tablets.</summary>
public static class DrugDose
{
    /// <summary>Works out a dose: weight (kg) x dose (mg/kg), then mg / concentration (mg/mL) and mg / tablet strength (mg).</summary>
    public static DrugDoseResult Calculate(double weightKg, double doseMgPerKg, double? concentrationMgPerMl = null, double? tabletStrengthMg = null)
    {
        Guard.Positive(weightKg, nameof(weightKg));
        Guard.NotNegative(doseMgPerKg, nameof(doseMgPerKg));
        if (concentrationMgPerMl is { } concentration)
        {
            Guard.Positive(concentration, nameof(concentrationMgPerMl));
        }

        if (tabletStrengthMg is { } strength)
        {
            Guard.Positive(strength, nameof(tabletStrengthMg));
        }

        var totalMg = weightKg * doseMgPerKg;
        return new DrugDoseResult(totalMg, totalMg / concentrationMgPerMl, totalMg / tabletStrengthMg);
    }

    /// <summary>
    /// Rounds a tablet count to the nearest fraction a tablet can be split into (2 for halves, 4 for quarters).
    /// </summary>
    public static double RoundToSplit(double tablets, int splitInto)
    {
        if (splitInto < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(splitInto), splitInto, "Must be at least 1.");
        }

        return Math.Round(tablets * splitInto, MidpointRounding.AwayFromZero) / splitInto;
    }

    /// <summary>
    /// A warning when rounding to a whole portion changes the dose by more than <paramref name="tolerance"/>
    /// (10% by default), such as 0.25 tablets rounding to a half (+100%) or 0.24 rounding to none; otherwise null.
    /// </summary>
    public static string? SplitWarning(double exactTablets, double roundedTablets, double tolerance = 0.1)
    {
        if (exactTablets <= 0)
        {
            return null;
        }

        if (roundedTablets == 0)
        {
            return "The dose is less than the smallest portion: consider a lower strength or a liquid.";
        }

        var change = (roundedTablets - exactTablets) / exactTablets;
        if (Math.Abs(change) <= tolerance)
        {
            return null;
        }

        return $"Rounding changes the dose by {(change > 0 ? "+" : "")}{change * 100:0}%: consider a finer split, another strength or a liquid.";
    }
}
