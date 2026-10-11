using ChocolateToxicity.Core;
using ChocolateToxicity.Core.Toxins;

namespace ChocolateToxicity.ViewModels;

/// <summary>What the result screen shows for one calculation, for chocolate or any other toxin.</summary>
public sealed class ResultViewModel
{
    /// <summary>A chocolate result.</summary>
    public ResultViewModel(ToxicityResult result, double dogWeightKg, double amountEatenGrams, string chocolateDescription)
    {
        Result = result;
        Band = result.Band;
        Risk = RiskFor(result.Band);
        EmesisIndicated = result.EmesisIndicated;
        Verdict = VerdictFor(result.EmesisIndicated);
        VerdictDetail = result.EmesisIndicated
            ? "The dose is at or above the 20 mg/kg threshold for clinical signs."
            : "The dose is below the 20 mg/kg threshold for clinical signs.";
        Dose = $"{NumberInput.FormatPrecise(result.DoseMgPerKg)} mg/kg";
        BandTitle = ClinicalText.BandTitle(result.Band);
        ExpectedSigns = ClinicalText.ExpectedSigns(result.Band);
        Summary = $"{Format(amountEatenGrams)} g of {chocolateDescription} eaten by a {Format(dogWeightKg)} kg dog.";
        Working =
            $"{NumberInput.FormatPrecise(result.MethylxanthineMgPerGram)} mg/g x {Format(amountEatenGrams)} g = " +
            $"{NumberInput.FormatPrecise(result.TotalMethylxanthineMg)} mg " +
            $"theobromine and caffeine, divided by {Format(dogWeightKg)} kg.";
        ThresholdsText = "Thresholds: mild signs from 20 mg/kg, severe (cardiotoxic) from 40 mg/kg, seizures from 60 mg/kg.";
        Disclaimer = ClinicalText.Disclaimer;
    }

    /// <summary>A result for one of the other toxins.</summary>
    /// <param name="assessment">The dose and the band it falls in.</param>
    /// <param name="summary">What was entered, such as "2 x 400 mg ibuprofen tablets eaten by a 10 kg dog."</param>
    /// <param name="working">How the dose was worked out.</param>
    public ResultViewModel(ToxinAssessment assessment, string summary, string working)
    {
        Assessment = assessment;
        var profile = assessment.Profile;
        Risk = assessment.Band.Risk;
        EmesisIndicated = assessment.EmesisIndicated;
        Verdict = VerdictFor(assessment.EmesisIndicated);
        VerdictDetail = profile.EmesisFromDose == 0
            ? $"There is no established safe dose of {profile.Name}."
            : assessment.EmesisIndicated
                ? $"The dose is at or above the {NumberInput.FormatPrecise(profile.EmesisFromDose)} {profile.DoseUnit} threshold for treatment."
                : $"The dose is below the {NumberInput.FormatPrecise(profile.EmesisFromDose)} {profile.DoseUnit} threshold for treatment.";
        Dose = $"{NumberInput.FormatPrecise(assessment.DosePerKg)} {profile.DoseUnit}";
        BandTitle = assessment.Band.Title;
        ExpectedSigns = assessment.Band.Signs;
        Summary = summary;
        Working = working;
        ThresholdsText = profile.ThresholdsText;
        Advice = profile.Advice;
        Disclaimer = ClinicalText.GeneralDisclaimer;
    }

    /// <summary>The chocolate calculation, for chocolate results.</summary>
    public ToxicityResult? Result { get; }

    /// <summary>The calculation, for the other toxins.</summary>
    public ToxinAssessment? Assessment { get; }

    /// <summary>The chocolate severity band, for chocolate results.</summary>
    public SeverityBand? Band { get; }

    public RiskLevel Risk { get; }

    public bool EmesisIndicated { get; }

    public string Verdict { get; }

    public string VerdictDetail { get; }

    public string Dose { get; }

    public string BandTitle { get; }

    public string ExpectedSigns { get; }

    public string Summary { get; }

    public string Working { get; }

    public string ThresholdsText { get; }

    /// <summary>Toxin-specific treatment notes, if any.</summary>
    public string? Advice { get; }

    public string Disclaimer { get; }

    private static string VerdictFor(bool emesisIndicated) =>
        emesisIndicated ? "Consider inducing emesis" : "Emesis not indicated by dose";

    private static RiskLevel RiskFor(SeverityBand band) => band switch
    {
        SeverityBand.Mild => RiskLevel.Moderate,
        SeverityBand.Severe => RiskLevel.High,
        SeverityBand.Seizures => RiskLevel.Critical,
        _ => RiskLevel.Low,
    };

    private static string Format(double value) => NumberInput.Format(value);
}
