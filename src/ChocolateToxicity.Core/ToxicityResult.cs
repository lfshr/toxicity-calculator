namespace ChocolateToxicity.Core;

/// <summary>Outcome of a chocolate toxicity calculation.</summary>
/// <param name="MethylxanthineMgPerGram">Theobromine plus caffeine content used for the chocolate, in mg/g.</param>
/// <param name="TotalMethylxanthineMg">Total theobromine plus caffeine eaten, in mg.</param>
/// <param name="DoseMgPerKg">Dose relative to the dog's weight, in mg/kg.</param>
/// <param name="Band">Expected clinical signs at this dose.</param>
public sealed record ToxicityResult(
    double MethylxanthineMgPerGram,
    double TotalMethylxanthineMg,
    double DoseMgPerKg,
    SeverityBand Band)
{
    /// <summary>
    /// True when the dose reaches the 20 mg/kg threshold for clinical signs, the point at which decontamination
    /// (inducing emesis) should be considered. Timing since ingestion and the patient's condition remain clinical decisions.
    /// </summary>
    public bool EmesisIndicated => Band >= SeverityBand.Mild;
}
