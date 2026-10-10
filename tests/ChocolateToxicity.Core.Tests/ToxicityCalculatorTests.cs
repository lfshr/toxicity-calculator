using ChocolateToxicity.Core;

namespace ChocolateToxicity.Core.Tests;

public class ToxicityCalculatorTests
{
    [Theory]
    [InlineData(0, SeverityBand.None)]
    [InlineData(19.99, SeverityBand.None)]
    [InlineData(20, SeverityBand.Mild)]
    [InlineData(39.99, SeverityBand.Mild)]
    [InlineData(40, SeverityBand.Severe)]
    [InlineData(59.99, SeverityBand.Severe)]
    [InlineData(60, SeverityBand.Seizures)]
    [InlineData(500, SeverityBand.Seizures)]
    public void Bands_follow_published_thresholds(double doseMgPerKg, SeverityBand expected)
    {
        Assert.Equal(expected, ToxicityThresholds.BandFor(doseMgPerKg));
    }

    [Fact]
    public void Dose_is_content_times_amount_divided_by_weight()
    {
        var result = ToxicityCalculator.Calculate(methylxanthineMgPerGram: 10, amountEatenGrams: 50, dogWeightKg: 20);

        Assert.Equal(500, result.TotalMethylxanthineMg, 6);
        Assert.Equal(25, result.DoseMgPerKg, 6);
        Assert.Equal(SeverityBand.Mild, result.Band);
        Assert.True(result.EmesisIndicated);
    }

    [Fact]
    public void Eating_nothing_gives_no_dose()
    {
        var result = ToxicityCalculator.Calculate(ChocolateType.CocoaPowder, 0, 10);

        Assert.Equal(0, result.DoseMgPerKg);
        Assert.False(result.EmesisIndicated);
    }

    [Theory]
    [InlineData(ChocolateType.White, 0.04)]
    [InlineData(ChocolateType.Milk, 2.3)]
    [InlineData(ChocolateType.MildDark, 5.6)]
    [InlineData(ChocolateType.Baking, 15.5)]
    [InlineData(ChocolateType.CocoaPowder, 28.5)]
    [InlineData(ChocolateType.CocoaBeanHulls, 9.0)]
    public void Type_content_matches_Merck(ChocolateType type, double expectedMgPerGram)
    {
        Assert.Equal(expectedMgPerGram, MethylxanthineContent.ForType(type));
    }

    [Fact]
    public void Strong_dark_uses_the_curve_at_seventy_percent()
    {
        Assert.Equal(
            MethylxanthineContent.ForCocoaPercent(MethylxanthineContent.StrongDarkAssumedCocoaPercent),
            MethylxanthineContent.ForType(ChocolateType.StrongDark));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(30, 2.3)]
    [InlineData(45, 5.6)]
    [InlineData(100, 15.5)]
    [InlineData(15, 1.15)]
    [InlineData(40, 4.5)]
    [InlineData(70, 10.1)]
    public void Cocoa_percent_curve_passes_through_Merck_anchors(double cocoaPercent, double expectedMgPerGram)
    {
        Assert.Equal(expectedMgPerGram, MethylxanthineContent.ForCocoaPercent(cocoaPercent), 6);
    }

    [Fact]
    public void Cocoa_percent_curve_never_decreases()
    {
        var previous = -1.0;
        for (var percent = 0.0; percent <= 100.0; percent += 0.5)
        {
            var content = MethylxanthineContent.ForCocoaPercent(percent);
            Assert.True(content >= previous, $"Content fell at {percent}%");
            previous = content;
        }
    }

    [Fact]
    public void Seventy_percent_dark_covers_the_Vets_Now_good_quality_bar()
    {
        // Vets Now: a good quality 500 g dark bar can contain up to 4000 mg theobromine (8 mg/g).
        Assert.True(MethylxanthineContent.ForCocoaPercent(70) >= 8.0);
    }

    [Fact]
    public void Cocoa_percent_and_type_give_the_same_result_at_anchor_points()
    {
        var fromPercent = ToxicityCalculator.CalculateFromCocoaPercent(30, 100, 10);
        var fromType = ToxicityCalculator.Calculate(ChocolateType.Milk, 100, 10);

        Assert.Equal(fromType.DoseMgPerKg, fromPercent.DoseMgPerKg, 6);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100.1)]
    [InlineData(double.NaN)]
    public void Cocoa_percent_out_of_range_is_rejected(double cocoaPercent)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MethylxanthineContent.ForCocoaPercent(cocoaPercent));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(double.NaN)]
    public void Non_positive_weight_is_rejected(double weightKg)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ToxicityCalculator.Calculate(ChocolateType.Milk, 100, weightKg));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    public void Negative_amount_is_rejected(double grams)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ToxicityCalculator.Calculate(ChocolateType.Milk, grams, 10));
    }

    [Fact]
    public void Negative_content_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ToxicityCalculator.Calculate(-1, 100, 10));
    }
}
