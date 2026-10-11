using ChocolateToxicity.Core;

namespace ChocolateToxicity.ViewModels;

/// <summary>Wording shown to the vet, kept in one place so screens and tests agree.</summary>
public static class ClinicalText
{
    public const string Disclaimer =
        "A decision aid for veterinary professionals, not a substitute for clinical judgement. " +
        "Individual animals vary in sensitivity and product content varies by brand and batch. " +
        "Consider time since ingestion, other ingredients (such as raisins, xylitol or macadamia nuts) " +
        "and the patient's condition before inducing emesis.";

    public const string GeneralDisclaimer =
        "A decision aid for veterinary professionals, not a substitute for clinical judgement. " +
        "Individual animals vary in sensitivity. Consider time since ingestion, anything else eaten " +
        "and the patient's condition before inducing emesis.";

    public static string BandTitle(SeverityBand band) => band switch
    {
        SeverityBand.None => "Below toxic dose",
        SeverityBand.Mild => "Mild signs expected",
        SeverityBand.Severe => "Severe signs expected",
        SeverityBand.Seizures => "Seizures possible",
        _ => throw new ArgumentOutOfRangeException(nameof(band), band, null),
    };

    public static string ExpectedSigns(SeverityBand band) => band switch
    {
        SeverityBand.None => "Methylxanthine toxicosis is not expected below 20 mg/kg. Fat and sugar may still cause vomiting or diarrhoea.",
        SeverityBand.Mild => "From 20 mg/kg: vomiting, diarrhoea, polydipsia, restlessness.",
        SeverityBand.Severe => "From 40 mg/kg: cardiotoxic effects such as tachycardia and arrhythmias, plus the mild signs.",
        SeverityBand.Seizures => "From 60 mg/kg: tremors and seizures, plus cardiotoxic effects. Potentially lethal from 100 mg/kg (oral LD50 of theobromine and caffeine 100-200 mg/kg).",
        _ => throw new ArgumentOutOfRangeException(nameof(band), band, null),
    };
}
