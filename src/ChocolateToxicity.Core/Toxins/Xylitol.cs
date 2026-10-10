namespace ChocolateToxicity.Core.Toxins;

/// <summary>Xylitol toxicosis in dogs. Dose is mg of xylitol per kg.</summary>
/// <remarks>
/// Merck Veterinary Manual, "Xylitol Toxicosis in Dogs"
/// (https://www.merckvetmanual.com/toxicology/food-hazards/xylitol-toxicosis-in-dogs): doses above about 100 mg/kg
/// have been associated with hypoglycaemia, and some dogs eating more than 500 mg/kg develop severe hepatic insufficiency
/// or failure. Emesis only under veterinary supervision in clinically normal patients; activated charcoal does not
/// appreciably bind xylitol.
/// </remarks>
public static class Xylitol
{
    public const double HypoglycaemiaMgPerKg = 100;
    public const double HepaticFailureMgPerKg = 500;

    public static ToxinProfile Profile { get; } = new(
        "xylitol",
        "mg/kg",
        new DoseBand(0, RiskLevel.Low, "Below toxic dose", "Hypoglycaemia is not expected below 100 mg/kg."),
        [
            new DoseBand(
                HypoglycaemiaMgPerKg,
                RiskLevel.High,
                "Hypoglycaemia risk",
                "From 100 mg/kg: hypoglycaemia, with vomiting, weakness, ataxia, collapse and seizures, which can start within an hour."),
            new DoseBand(
                HepaticFailureMgPerKg,
                RiskLevel.Critical,
                "Liver failure risk",
                "From 500 mg/kg: acute hepatic necrosis and liver failure, as well as hypoglycaemia."),
        ],
        emesisFromDose: HypoglycaemiaMgPerKg,
        "Thresholds: hypoglycaemia from 100 mg/kg, liver failure from 500 mg/kg (Merck Veterinary Manual).",
        "Check blood glucose first: only induce emesis in a dog that is clinically normal. Activated charcoal does not bind xylitol.");

    /// <summary>Total xylitol eaten, in mg, from the amount of product and the xylitol content on its label.</summary>
    public static double MgEaten(double productEatenGrams, double xylitolGramsPer100g)
    {
        if (double.IsNaN(productEatenGrams) || productEatenGrams < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productEatenGrams), productEatenGrams, "Amount eaten cannot be negative.");
        }

        if (double.IsNaN(xylitolGramsPer100g) || xylitolGramsPer100g < 0 || xylitolGramsPer100g > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(xylitolGramsPer100g), xylitolGramsPer100g, "Xylitol content must be between 0 and 100 g per 100 g.");
        }

        return productEatenGrams * xylitolGramsPer100g / 100 * 1000;
    }

    public static ToxinAssessment Assess(double xylitolMg, double dogWeightKg) => Profile.Assess(Dose.PerKg(xylitolMg, dogWeightKg));
}
