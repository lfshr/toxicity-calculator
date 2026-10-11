using ChocolateToxicity.Core.Toxins;

namespace ChocolateToxicity.Core.Tests;

/// <summary>Cases from the second maths audit.</summary>
public class MathAudit2Tests
{
    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(15)]
    [InlineData(19.9)]
    public void A_stated_cocoa_percent_below_20_never_gets_less_than_milk_chocolate(double cocoaPercent)
    {
        Assert.True(MethylxanthineContent.ForCocoaPercent(cocoaPercent) >= MethylxanthineContent.Milk);
    }

    [Fact]
    public void An_11_percent_bar_gives_the_same_verdict_as_milk_chocolate()
    {
        // US milk chocolate can be 10-15% cocoa: 100 g in a 10 kg dog is 23 mg/kg, the same as picking milk by type.
        var byPercent = ToxicityCalculator.CalculateFromCocoaPercent(11, 100, 10);
        var byType = ToxicityCalculator.Calculate(ChocolateType.Milk, 100, 10);

        Assert.Equal(SeverityBand.Mild, byPercent.Band);
        Assert.True(byPercent.EmesisIndicated);
        Assert.Equal(byType.DoseMgPerKg, byPercent.DoseMgPerKg, 6);
    }

    [Fact]
    public void Cholecalciferol_onset_matches_Merck()
    {
        var signs = Rodenticides.Cholecalciferol.Bands
            .Select(b => b.Signs);

        Assert.Contains(signs, s => s.Contains("12-48 hours", StringComparison.Ordinal));
    }

    [Fact]
    public void Ibuprofen_cites_Dunayer_for_the_25_mg_per_kg_threshold()
    {
        Assert.Contains("Dunayer", Painkillers.Ibuprofen.ThresholdsText, StringComparison.Ordinal);
    }
}
