namespace ChocolateToxicity.Clinical;

/// <summary>Body surface area from body weight.</summary>
/// <remarks>
/// Merck Veterinary Manual, "Weight to Body Surface Area Conversion for Dogs" and "... for Cats":
/// BSA (m²) = K x (body weight in g)^(2/3) x 10^-4, with K = 10.1 for dogs and 10.0 for cats.
/// </remarks>
public static class BodySurfaceArea
{
    public const double DogK = 10.1;
    public const double CatK = 10.0;

    public static double SquareMetres(Species species, double weightKg)
    {
        Guard.Positive(weightKg, nameof(weightKg));
        var k = species == Species.Cat ? CatK : DogK;
        return k * Math.Pow(weightKg * 1000, 2.0 / 3.0) / 10_000;
    }
}
