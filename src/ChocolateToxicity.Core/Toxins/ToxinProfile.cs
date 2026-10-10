namespace ChocolateToxicity.Core.Toxins;

/// <summary>
/// The published dog thresholds for one toxin: the dose bands in increasing order and the dose from which
/// decontamination (inducing emesis) should be considered.
/// </summary>
public sealed class ToxinProfile
{
    public ToxinProfile(
        string name,
        string doseUnit,
        DoseBand belowThresholds,
        IReadOnlyList<DoseBand> bands,
        double emesisFromDose,
        string thresholdsText,
        string? advice = null)
    {
        if (bands.Count == 0 || bands.Zip(bands.Skip(1)).Any(pair => pair.First.FromDose >= pair.Second.FromDose))
        {
            throw new ArgumentException("Bands must be given in increasing order of dose.", nameof(bands));
        }

        Name = name;
        DoseUnit = doseUnit;
        BelowThresholds = belowThresholds;
        Bands = bands;
        EmesisFromDose = emesisFromDose;
        ThresholdsText = thresholdsText;
        Advice = advice;
    }

    /// <summary>Lower-case name for use mid-sentence, such as "xylitol".</summary>
    public string Name { get; }

    /// <summary>Unit of the dose per kg, such as "mg/kg".</summary>
    public string DoseUnit { get; }

    /// <summary>What to expect below the lowest band.</summary>
    public DoseBand BelowThresholds { get; }

    /// <summary>Dose bands in increasing order.</summary>
    public IReadOnlyList<DoseBand> Bands { get; }

    /// <summary>
    /// Dose from which inducing emesis should be considered. Zero means any ingestion, for toxins with no safe dose.
    /// </summary>
    public double EmesisFromDose { get; }

    /// <summary>The thresholds and their source, as shown under the result.</summary>
    public string ThresholdsText { get; }

    /// <summary>Toxin-specific decontamination advice, if any.</summary>
    public string? Advice { get; }

    public ToxinAssessment Assess(double dosePerKg)
    {
        if (double.IsNaN(dosePerKg) || dosePerKg < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dosePerKg), dosePerKg, "Dose cannot be negative.");
        }

        var band = Bands.LastOrDefault(b => dosePerKg >= b.FromDose) ?? BelowThresholds;
        var emesis = dosePerKg > 0 && dosePerKg >= EmesisFromDose;
        return new ToxinAssessment(this, dosePerKg, band, emesis);
    }
}
