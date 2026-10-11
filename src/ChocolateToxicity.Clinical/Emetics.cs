namespace ChocolateToxicity.Clinical;

/// <summary>A Clevor (ropinirole) dose from the product's dosing table.</summary>
/// <param name="Drops">Number of drops per dose.</param>
/// <param name="Micrograms">Ropinirole in that many drops.</param>
/// <param name="Instructions">How to split the drops between the eyes.</param>
public sealed record RopiniroleDose(int Drops, double Micrograms, string Instructions);

/// <summary>Doses for inducing emesis in dogs, and in cats with dexmedetomidine.</summary>
public static class Emetics
{
    /// <summary>
    /// Apomorphine by subcutaneous injection, per the UK SPC for Emedog 1 mg/ml (Vm 54982/3002): 0.1 mg/kg SC, which
    /// is 0.1 mL/kg of the 1 mg/mL solution.
    /// </summary>
    public const double ApomorphineSubcutaneousMgPerKg = 0.1;

    /// <summary>Strength of the licensed UK apomorphine solutions (Emedog, Apometic), mg/mL.</summary>
    public const double ApomorphineConcentrationMgPerMl = 1.0;

    /// <summary>Merck Veterinary Manual, Chocolate Toxicosis: apomorphine 0.03-0.04 mg/kg, IV preferred for speed.</summary>
    public const double ApomorphineIntravenousMinMgPerKg = 0.03;

    public const double ApomorphineIntravenousMaxMgPerKg = 0.04;

    /// <summary>Merck Veterinary Manual, Chocolate Toxicosis: a single dose of activated charcoal, 1-2 g/kg PO.</summary>
    public const double CharcoalMinGramsPerKg = 1;

    public const double CharcoalMaxGramsPerKg = 2;

    /// <summary>
    /// Merck Veterinary Manual, Chocolate Toxicosis: dexmedetomidine for emesis in cats, 7-40 µg/kg IM or 3.5 µg/kg IV.
    /// </summary>
    public const double CatDexmedetomidineImMinMicrogramsPerKg = 7;

    public const double CatDexmedetomidineImMaxMicrogramsPerKg = 40;

    public const double CatDexmedetomidineIvMicrogramsPerKg = 3.5;

    /// <summary>Strength of the usual dexmedetomidine solution (Dexdomitor 0.5 mg/ml), in µg/mL. Check the vial.</summary>
    public const double DexmedetomidineMicrogramsPerMl = 500;

    /// <summary>Clevor 30 mg/ml eye drops: each drop is about 27 µL and contains 810 µg of ropinirole.</summary>
    public const double RopiniroleMicrogramsPerDrop = 810;

    public const double ClevorMinWeightKg = 1.8;
    public const double ClevorMaxWeightKg = 100;

    // Clevor SPC section 3.9 (VMD SPC 3016241): drops by body weight, aimed at 3.75 mg/m² (range 2.7-5.4 mg/m²).
    private static readonly (double MaxKg, int Drops)[] ClevorTable =
    [
        (5, 1),
        (10, 2),
        (20, 3),
        (35, 4),
        (60, 6),
        (100, 8),
    ];

    /// <summary>
    /// Clevor drops for a dog's weight, from the SPC table. Returns null outside 1.8-100 kg, where the SPC gives no dose.
    /// </summary>
    public static RopiniroleDose? Ropinirole(double weightKg)
    {
        Guard.Positive(weightKg, nameof(weightKg));
        if (weightKg < ClevorMinWeightKg || weightKg > ClevorMaxWeightKg)
        {
            return null;
        }

        // The SPC's bands are 1.8-5, 5.1-10, 10.1-20 and so on, so weights between bands round to the lower one.
        var drops = ClevorTable.First(row => weightKg < row.MaxKg + 0.1).Drops;
        var instructions = drops switch
        {
            1 => "Give the drop into one eye.",
            <= 4 => "Divide the drops between both eyes.",
            _ => $"Give {drops / 2} drops (split between both eyes), then the other {drops / 2} 1-2 minutes later.",
        };

        return new RopiniroleDose(drops, drops * RopiniroleMicrogramsPerDrop, instructions);
    }
}
