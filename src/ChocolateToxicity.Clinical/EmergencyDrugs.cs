namespace ChocolateToxicity.Clinical;

/// <summary>A CPR or reversal drug with its dose and the concentration it usually comes in.</summary>
/// <param name="Name">Drug name.</param>
/// <param name="Use">When it's given.</param>
/// <param name="DosePerKg">Dose per kg, in <paramref name="Unit"/>.</param>
/// <param name="Unit">"mg" or "U".</param>
/// <param name="ConcentrationPerMl">Usual stock concentration, in <paramref name="Unit"/> per mL. Check the vial.</param>
public sealed record EmergencyDrug(string Name, string Use, double DosePerKg, string Unit, double ConcentrationPerMl)
{
    public double Dose(double weightKg) => DosePerKg * weightKg;

    public double VolumeMl(double weightKg, double concentrationPerMl) => Dose(weightKg) / concentrationPerMl;
}

/// <summary>CPR drug doses and defibrillation energies for dogs and cats.</summary>
/// <remarks>
/// RECOVER 2024 drug dosing chart (https://recoverinitiative.org), which gives the same per-kg doses for dogs and cats.
/// </remarks>
public static class EmergencyDrugs
{
    public static IReadOnlyList<EmergencyDrug> All { get; } =
    [
        new("Adrenaline (epinephrine), low dose", "Arrest: every other BLS cycle (every 3-5 minutes)", 0.01, "mg", 1),
        new("Vasopressin", "Arrest: may replace or alternate with adrenaline", 0.8, "U", 20),
        new("Atropine", "Arrest with suspected high vagal tone", 0.05, "mg", 0.54),
        new("Amiodarone", "Refractory VF or pulseless VT", 5, "mg", 50),
        new("Lidocaine", "Refractory VF or pulseless VT, if amiodarone is unavailable", 2, "mg", 20),
        new("Esmolol", "Refractory VF or pulseless VT, over 3-5 minutes, then a 50 µg/kg/min CRI", 0.5, "mg", 10),
        new("Naloxone", "Reverses opioids", 0.04, "mg", 0.4),
        new("Flumazenil", "Reverses benzodiazepines", 0.01, "mg", 0.1),
        new("Atipamezole", "Reverses alpha-2 agonists (medetomidine, dexmedetomidine)", 0.1, "mg", 5),
    ];

    /// <summary>External defibrillation energy range, in joules, for a monophasic or biphasic defibrillator.</summary>
    public static (double MinJoules, double MaxJoules) ExternalDefibrillation(double weightKg, bool biphasic)
    {
        Guard.Positive(weightKg, nameof(weightKg));
        return biphasic ? (2 * weightKg, 4 * weightKg) : (4 * weightKg, 6 * weightKg);
    }

    /// <summary>Internal (open chest) defibrillation energy range, in joules.</summary>
    public static (double MinJoules, double MaxJoules) InternalDefibrillation(double weightKg, bool biphasic)
    {
        Guard.Positive(weightKg, nameof(weightKg));
        return biphasic ? (0.2 * weightKg, 0.4 * weightKg) : (0.5 * weightKg, 1 * weightKg);
    }
}
