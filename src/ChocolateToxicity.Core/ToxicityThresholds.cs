namespace ChocolateToxicity.Core;

/// <summary>
/// Methylxanthine doses in dogs at which clinical signs are expected, in mg per kg of body weight.
/// </summary>
/// <remarks>
/// Merck Veterinary Manual: mild signs at 20 mg/kg, cardiotoxic effects at 40-50 mg/kg, seizures at 60 mg/kg or more.
/// The primary source gives the same figures: Gwaltney-Brant, "Chocolate intoxication", Veterinary Medicine, February 2001
/// (https://www.aspcapro.org/sites/default/files/m-toxbrief_0201.pdf). Vets Now is the design reference for the
/// calculator, not a source for the thresholds.
/// </remarks>
public static class ToxicityThresholds
{
    public const double MildMgPerKg = 20.0;
    public const double SevereMgPerKg = 40.0;
    public const double SeizuresMgPerKg = 60.0;

    /// <summary>The band for a dose, compared at the precision it is displayed at (see <see cref="DisplayedDose"/>).</summary>
    public static SeverityBand BandFor(double doseMgPerKg) => DisplayedDose.Round(doseMgPerKg) switch
    {
        >= SeizuresMgPerKg => SeverityBand.Seizures,
        >= SevereMgPerKg => SeverityBand.Severe,
        >= MildMgPerKg => SeverityBand.Mild,
        _ => SeverityBand.None,
    };
}
