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
/// RECOVER 2024 drug dosing chart (https://recoverinitiative.org), which gives the same per-kg doses for dogs and cats,
/// and the 2024 RECOVER guidelines (Burkitt-Creedon et al., J Vet Emerg Crit Care 2024, doi:10.1111/vec.13391):
/// for refractory VF or pulseless VT, lidocaine is the first choice in dogs and amiodarone the alternative, while
/// lidocaine is not recommended in cats because of their sensitivity to its cardiovascular and CNS effects; atropine
/// is given as a single dose; the first biphasic shock is 2 J/kg, doubled once to 4 J/kg if it fails.
/// </remarks>
public static class EmergencyDrugs
{
    private static readonly EmergencyDrug Adrenaline =
        new("Adrenaline (epinephrine), low dose", "Arrest: every other BLS cycle (every 3-5 minutes)", 0.01, "mg", 1);

    private static readonly EmergencyDrug Vasopressin =
        new("Vasopressin", "Arrest: may replace or alternate with adrenaline", 0.8, "U", 20);

    private static readonly EmergencyDrug Atropine =
        new("Atropine", "Arrest with suspected high vagal tone: a single dose, not repeated", 0.05, "mg", 0.54);

    private static readonly EmergencyDrug Esmolol =
        new("Esmolol", "Refractory VF or pulseless VT, over 3-5 minutes, then a 50 µg/kg/min CRI", 0.5, "mg", 10);

    private static readonly EmergencyDrug[] Reversals =
    [
        new("Naloxone", "Reverses opioids", 0.04, "mg", 0.4),
        new("Flumazenil", "Reverses benzodiazepines", 0.01, "mg", 0.1),
        new("Atipamezole", "Reverses alpha-2 agonists (medetomidine, dexmedetomidine)", 0.1, "mg", 5),
    ];

    /// <summary>The CPR drugs for dogs: lidocaine first for refractory VF or pulseless VT, then amiodarone.</summary>
    public static IReadOnlyList<EmergencyDrug> Dog { get; } =
    [
        Adrenaline,
        Vasopressin,
        Atropine,
        new("Lidocaine", "Refractory VF or pulseless VT: first choice in dogs", 2, "mg", 20),
        new("Amiodarone", "Refractory VF or pulseless VT, if lidocaine is unavailable", 5, "mg", 50),
        Esmolol,
        .. Reversals,
    ];

    /// <summary>The CPR drugs for cats: amiodarone for refractory VF or pulseless VT, and no lidocaine.</summary>
    public static IReadOnlyList<EmergencyDrug> Cat { get; } =
    [
        Adrenaline,
        Vasopressin,
        Atropine,
        new("Amiodarone", "Refractory VF or pulseless VT: preferred in cats", 5, "mg", 50),
        Esmolol,
        .. Reversals,
    ];

    /// <summary>The dog list, for callers that don't know the species.</summary>
    public static IReadOnlyList<EmergencyDrug> All => Dog;

    public static IReadOnlyList<EmergencyDrug> For(Species species) => species == Species.Cat ? Cat : Dog;

    /// <summary>
    /// External defibrillation energy in joules: the first shock, and later shocks if the first fails. Biphasic is
    /// 2 J/kg then 4 J/kg (RECOVER 2024 doubles the energy once and advises against going further). Monophasic is
    /// 4 J/kg then 6 J/kg, the bottom and top of the RECOVER 2024 range.
    /// </summary>
    public static (double FirstJoules, double LaterJoules) ExternalDefibrillation(double weightKg, bool biphasic)
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
