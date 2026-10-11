using ChocolateToxicity.Core;

namespace ChocolateToxicity.Core.Tests;

/// <summary>
/// Worked examples from Vets Now, "My dog has eaten chocolate"
/// (https://www.vets-now.com/pet-care-advice/dogs/dog-eaten-chocolate).
/// Vets Now describes the milk chocolate examples for a "Labrador-sized" dog; 25 kg is used here.
/// </summary>
public class VetsNowExampleTests
{
    private const double LabradorKg = 25.0;

    [Fact]
    public void Labrador_eating_500g_milk_chocolate_has_cardiovascular_signs()
    {
        // "500g [of milk chocolate]: cardiovascular problems and increased heart rate will be seen."
        var result = ToxicityCalculator.Calculate(ChocolateType.Milk, 500, LabradorKg);

        Assert.Equal(46.0, result.DoseMgPerKg, 1);
        Assert.Equal(SeverityBand.Severe, result.Band);
        Assert.True(result.EmesisIndicated);
    }

    [Fact]
    public void Labrador_eating_750g_milk_chocolate_may_have_seizures()
    {
        // "Eating 750g may result in seizures."
        var result = ToxicityCalculator.Calculate(ChocolateType.Milk, 750, LabradorKg);

        Assert.Equal(69.0, result.DoseMgPerKg, 1);
        Assert.Equal(SeverityBand.Seizures, result.Band);
    }

    [Fact]
    public void Labrador_eating_200g_milk_chocolate_is_just_under_the_mild_threshold()
    {
        // Vets Now: 200g "is likely to have a stomach upset such as vomiting and diarrhoea".
        // With Merck's 2.3 mg/g this is 18.4 mg/kg, just below 20 mg/kg; the stomach upset at this amount
        // is plausibly from fat and sugar rather than methylxanthine toxicosis.
        var result = ToxicityCalculator.Calculate(ChocolateType.Milk, 200, LabradorKg);

        Assert.Equal(18.4, result.DoseMgPerKg, 1);
        Assert.Equal(SeverityBand.None, result.Band);
    }

    [Fact]
    public void Three_and_a_half_grams_of_dark_chocolate_per_kg_reaches_the_toxic_threshold()
    {
        // "Owners whose dogs have eaten 3.5g of dark chocolate for every 1kg they weigh ... should consult their vet."
        const double dogKg = 10;
        var result = ToxicityCalculator.Calculate(ChocolateType.MildDark, 3.5 * dogKg, dogKg);

        // Mild dark is worked out at the top of its 35-50% band, 6.5 mg/g, so this errs just above the threshold.
        Assert.InRange(result.DoseMgPerKg, ToxicityThresholds.MildMgPerKg, ToxicityThresholds.MildMgPerKg * 1.15);
        Assert.Equal(SeverityBand.Mild, result.Band);
    }

    [Fact]
    public void Thirty_kg_Labrador_eating_500g_good_quality_dark_bar_is_life_threatening()
    {
        // "A good quality 500g bar of dark chocolate can contain as much as 4000 mg of theobromine.
        // That's more than enough to kill a 30kg dog such as a Labrador."
        var result = ToxicityCalculator.Calculate(ChocolateType.StrongDark, 500, 30);

        Assert.True(result.TotalMethylxanthineMg >= 4000);
        Assert.Equal(SeverityBand.Seizures, result.Band);
    }

    [Fact]
    public void Lower_grade_dark_bar_covers_the_Vets_Now_theobromine_figure()
    {
        // "a 500g bar with 45 to 59% cacao solids can contain up to 2500mg of theobromine."
        foreach (var cocoaPercent in new[] { 45.0, 52.0, 59.0 })
        {
            var result = ToxicityCalculator.CalculateFromCocoaPercent(cocoaPercent, 500, 30);
            Assert.True(result.TotalMethylxanthineMg >= 2500, $"{cocoaPercent}% gave {result.TotalMethylxanthineMg} mg");
        }
    }

    [Fact]
    public void Cocoa_powder_covers_the_Vets_Now_theobromine_figure()
    {
        // "Dry cocoa powder contains up to 26mg of theobromine per gram."
        Assert.True(MethylxanthineContent.ForType(ChocolateType.CocoaPowder) >= 26);
    }

    [Fact]
    public void White_chocolate_is_not_a_methylxanthine_risk()
    {
        // White chocolate is "highly unlikely" to poison dogs: a small dog eating a large bar stays far below 20 mg/kg.
        var result = ToxicityCalculator.Calculate(ChocolateType.White, 200, 5);

        Assert.Equal(SeverityBand.None, result.Band);
        Assert.False(result.EmesisIndicated);
    }
}
