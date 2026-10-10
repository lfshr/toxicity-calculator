using System.Globalization;
using ChocolateToxicity.Core;

namespace ChocolateToxicity.ViewModels;

/// <summary>What the result screen shows for one calculation.</summary>
public sealed class ResultViewModel
{
    public ResultViewModel(ToxicityResult result, double dogWeightKg, double amountEatenGrams, string chocolateDescription)
    {
        Result = result;
        Band = result.Band;
        EmesisIndicated = result.EmesisIndicated;
        Verdict = result.EmesisIndicated ? "Consider inducing emesis" : "Emesis not indicated by dose";
        VerdictDetail = result.EmesisIndicated
            ? "The dose is at or above the 20 mg/kg threshold for clinical signs."
            : "The dose is below the 20 mg/kg threshold for clinical signs.";
        Dose = $"{Format(result.DoseMgPerKg)} mg/kg";
        BandTitle = ClinicalText.BandTitle(result.Band);
        ExpectedSigns = ClinicalText.ExpectedSigns(result.Band);
        Summary = $"{Format(amountEatenGrams)} g of {chocolateDescription} eaten by a {Format(dogWeightKg)} kg dog.";
        Working =
            $"{Format(result.MethylxanthineMgPerGram)} mg/g x {Format(amountEatenGrams)} g = {Format(result.TotalMethylxanthineMg)} mg " +
            $"theobromine and caffeine, divided by {Format(dogWeightKg)} kg.";
    }

    public ToxicityResult Result { get; }

    public SeverityBand Band { get; }

    public bool EmesisIndicated { get; }

    public string Verdict { get; }

    public string VerdictDetail { get; }

    public string Dose { get; }

    public string BandTitle { get; }

    public string ExpectedSigns { get; }

    public string Summary { get; }

    public string Working { get; }

    public string Disclaimer => ClinicalText.Disclaimer;

    // Up to one decimal place, dropping a trailing ".0", so "12 kg" and "52.3 mg/kg".
    private static string Format(double value) => value.ToString("0.#", CultureInfo.CurrentCulture);
}
