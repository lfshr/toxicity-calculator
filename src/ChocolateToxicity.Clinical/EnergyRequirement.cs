namespace ChocolateToxicity.Clinical;

/// <summary>A life stage or situation with its usual multiple of the resting energy requirement.</summary>
public sealed record EnergyFactor(Species Species, string Name, double Factor);

/// <summary>Resting and maintenance energy requirements.</summary>
/// <remarks>
/// RER (kcal/day) = 70 x BW(kg)^0.75, as in the AAHA 2021 Nutrition and Weight Management
/// Guidelines ("Energy Requirement Calculations", box 1). Every multiple here sits within AAHA's ranges; individual
/// needs vary widely, so intake should be adjusted to body condition. Hospitalised patients are usually fed at RER.
/// </remarks>
public static class EnergyRequirement
{
    public static double RestingKcalPerDay(double weightKg)
    {
        Guard.Positive(weightKg, nameof(weightKg));
        return 70 * Math.Pow(weightKg, 0.75);
    }

    public static IReadOnlyList<EnergyFactor> Factors { get; } =
    [
        new(Species.Dog, "Hospitalised (feed at RER)", 1.0),
        new(Species.Dog, "Neutered adult", 1.6),
        new(Species.Dog, "Intact adult", 1.8),
        new(Species.Dog, "Inactive or obesity prone", 1.2),
        new(Species.Dog, "Weight loss (at target weight)", 1.0),
        new(Species.Dog, "Puppy under 4 months", 3.0),
        new(Species.Dog, "Puppy over 4 months", 2.0),
        new(Species.Cat, "Hospitalised (feed at RER)", 1.0),
        new(Species.Cat, "Neutered adult", 1.2),
        new(Species.Cat, "Intact adult", 1.4),
        new(Species.Cat, "Obesity prone", 1.0),
        new(Species.Cat, "Weight loss (at target weight)", 0.8),
        new(Species.Cat, "Kitten", 2.5),
    ];
}
