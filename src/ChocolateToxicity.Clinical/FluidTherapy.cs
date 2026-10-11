namespace ChocolateToxicity.Clinical;

/// <summary>An hourly fluid plan for the first period of therapy.</summary>
/// <param name="MaintenanceMlPerDay">Maintenance requirement, mL/day.</param>
/// <param name="DeficitMl">Volume to replace the estimated dehydration, mL.</param>
/// <param name="MaintenanceMlPerHour">Maintenance part of the rate, mL/hr.</param>
/// <param name="DeficitMlPerHour">Deficit replacement part of the rate, mL/hr, while the deficit is being replaced.</param>
/// <param name="OngoingLossesMlPerHour">Estimated ongoing losses (vomiting, diarrhoea, polyuria), mL/hr.</param>
public sealed record FluidPlan(
    double MaintenanceMlPerDay,
    double DeficitMl,
    double MaintenanceMlPerHour,
    double DeficitMlPerHour,
    double OngoingLossesMlPerHour)
{
    /// <summary>Total rate while the deficit is being replaced, mL/hr.</summary>
    public double TotalMlPerHour => MaintenanceMlPerHour + DeficitMlPerHour + OngoingLossesMlPerHour;

    /// <summary>Rate once the deficit has been replaced, mL/hr.</summary>
    public double AfterDeficitMlPerHour => MaintenanceMlPerHour + OngoingLossesMlPerHour;
}

/// <summary>Maintenance fluids, dehydration deficit and drip rates.</summary>
/// <remarks>
/// AAHA 2013 Fluid Therapy Guidelines for Dogs and Cats, with the same formulas in AAHA's current maintenance fluid
/// rate table (https://aaha.org/aaha-guidelines/fluid-therapy/quick-reference-fluid-tables/maintenance-fluid-rates/): maintenance is 132 x BW(kg)^0.75 mL/day for dogs and
/// 80 x BW(kg)^0.75 mL/day for cats (https://www.aaha.org/aaha-guidelines/fluid-therapy/fluids-for-maintenance-and-replacement/).
/// The deficit is the standard estimate: % dehydration x body weight (kg) x 1000 mL.
/// </remarks>
public static class FluidTherapy
{
    public const double DogMaintenanceFactor = 132;
    public const double CatMaintenanceFactor = 80;

    /// <summary>Daily maintenance fluid requirement in mL, from the AAHA allometric formulas.</summary>
    public static double MaintenanceMlPerDay(Species species, double weightKg)
    {
        Guard.Positive(weightKg, nameof(weightKg));
        var factor = species == Species.Cat ? CatMaintenanceFactor : DogMaintenanceFactor;
        return factor * Math.Pow(weightKg, 0.75);
    }

    /// <summary>Fluid deficit in mL: dehydration (%) / 100 x weight (kg) x 1000.</summary>
    public static double DeficitMl(double dehydrationPercent, double weightKg)
    {
        Guard.Positive(weightKg, nameof(weightKg));
        if (!double.IsFinite(dehydrationPercent) || dehydrationPercent < 0 || dehydrationPercent > 15)
        {
            throw new ArgumentOutOfRangeException(nameof(dehydrationPercent), dehydrationPercent, "Dehydration must be between 0 and 15%.");
        }

        return dehydrationPercent * weightKg * 10;
    }

    /// <summary>Builds an hourly plan from maintenance, a deficit replaced over a number of hours, and ongoing losses.</summary>
    public static FluidPlan Plan(Species species, double weightKg, double dehydrationPercent, double replaceDeficitOverHours, double ongoingLossesMlPerDay = 0)
    {
        Guard.Positive(replaceDeficitOverHours, nameof(replaceDeficitOverHours));
        Guard.NotNegative(ongoingLossesMlPerDay, nameof(ongoingLossesMlPerDay));
        var maintenance = MaintenanceMlPerDay(species, weightKg);
        var deficit = DeficitMl(dehydrationPercent, weightKg);
        return new FluidPlan(maintenance, deficit, maintenance / 24, deficit / replaceDeficitOverHours, ongoingLossesMlPerDay / 24);
    }

    /// <summary>Drops per minute for a gravity giving set: mL/hr x drops per mL / 60.</summary>
    public static double DropsPerMinute(double mlPerHour, double dropsPerMl)
    {
        Guard.NotNegative(mlPerHour, nameof(mlPerHour));
        Guard.Positive(dropsPerMl, nameof(dropsPerMl));
        return mlPerHour * dropsPerMl / 60;
    }
}
