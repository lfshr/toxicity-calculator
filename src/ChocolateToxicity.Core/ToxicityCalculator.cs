namespace ChocolateToxicity.Core;

/// <summary>Calculates the methylxanthine dose a dog has eaten and the expected severity.</summary>
public static class ToxicityCalculator
{
    /// <summary>Calculates toxicity from a chocolate category.</summary>
    public static ToxicityResult Calculate(ChocolateType type, double amountEatenGrams, double dogWeightKg) =>
        Calculate(MethylxanthineContent.ForType(type), amountEatenGrams, dogWeightKg);

    /// <summary>Calculates toxicity from the cocoa percentage on a chocolate bar's label.</summary>
    public static ToxicityResult CalculateFromCocoaPercent(double cocoaPercent, double amountEatenGrams, double dogWeightKg) =>
        Calculate(MethylxanthineContent.ForCocoaPercent(cocoaPercent), amountEatenGrams, dogWeightKg);

    /// <summary>Calculates toxicity from a known methylxanthine content in mg/g.</summary>
    public static ToxicityResult Calculate(double methylxanthineMgPerGram, double amountEatenGrams, double dogWeightKg)
    {
        if (double.IsNaN(methylxanthineMgPerGram) || methylxanthineMgPerGram < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(methylxanthineMgPerGram), methylxanthineMgPerGram, "Methylxanthine content cannot be negative.");
        }

        if (double.IsNaN(amountEatenGrams) || amountEatenGrams < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amountEatenGrams), amountEatenGrams, "Amount eaten cannot be negative.");
        }

        if (double.IsNaN(dogWeightKg) || dogWeightKg <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dogWeightKg), dogWeightKg, "Dog weight must be greater than zero.");
        }

        var totalMg = methylxanthineMgPerGram * amountEatenGrams;
        var dose = totalMg / dogWeightKg;
        return new ToxicityResult(methylxanthineMgPerGram, totalMg, dose, ToxicityThresholds.BandFor(dose));
    }
}
