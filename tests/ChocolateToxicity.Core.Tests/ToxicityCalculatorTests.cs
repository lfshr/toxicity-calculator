using ChocolateToxicity.Core;

namespace ChocolateToxicity.Core.Tests;

public class ToxicityCalculatorTests
{
    [Theory]
    [InlineData(0, SeverityBand.None)]
    [InlineData(19.94, SeverityBand.None)]
    [InlineData(20, SeverityBand.Mild)]
    [InlineData(39.94, SeverityBand.Mild)]
    [InlineData(40, SeverityBand.Severe)]
    [InlineData(59.94, SeverityBand.Severe)]
    [InlineData(60, SeverityBand.Seizures)]
    [InlineData(500, SeverityBand.Seizures)]
    public void Bands_follow_published_thresholds(double doseMgPerKg, SeverityBand expected)
    {
        Assert.Equal(expected, ToxicityThresholds.BandFor(doseMgPerKg));
    }

    [Theory]
    [InlineData(19.96)]
    [InlineData(19.999999999999996)]
    public void A_dose_shown_as_20_is_banded_as_20(double doseMgPerKg)
    {
        Assert.Equal("20", DisplayedDose.Format(doseMgPerKg, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(SeverityBand.Mild, ToxicityThresholds.BandFor(doseMgPerKg));
    }

    [Theory]
    [InlineData(100, 11.5)]
    [InlineData(200, 23)]
    public void Milk_chocolate_exactly_at_20_mg_per_kg_is_mild(double grams, double dogKg)
    {
        // 2.3 x 100 / 11.5 is 20 on paper but 19.999999999999996 in floating point.
        var result = ToxicityCalculator.Calculate(ChocolateType.Milk, grams, dogKg);

        Assert.Equal(SeverityBand.Mild, result.Band);
        Assert.True(result.EmesisIndicated);
    }

    [Fact]
    public void A_low_cocoa_milk_bar_gets_at_least_the_milk_chocolate_content()
    {
        // A UK milk bar labelled 26% cocoa: by percentage it must not come out lower than "Milk chocolate" by type.
        Assert.True(MethylxanthineContent.ForCocoaPercent(26) >= MethylxanthineContent.Milk);
        Assert.Equal(SeverityBand.Mild, ToxicityCalculator.CalculateFromCocoaPercent(26, 100, 10).Band);
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
    [InlineData(ChocolateType.Baking, 15.5)]
    [InlineData(ChocolateType.CocoaPowder, 28.5)]
    [InlineData(ChocolateType.CocoaBeanHulls, 9.0)]
    public void Type_content_matches_Merck(ChocolateType type, double expectedMgPerGram)
    {
        Assert.Equal(expectedMgPerGram, MethylxanthineContent.ForType(type));
    }

    [Fact]
    public void Mild_dark_uses_the_top_of_its_band()
    {
        // 35-50% cocoa: the type gives what a 50% bar gets from its cocoa percentage, 6.5 mg/g.
        Assert.Equal(MethylxanthineContent.ForCocoaPercent(50), MethylxanthineContent.ForType(ChocolateType.MildDark));
        Assert.Equal(6.5, MethylxanthineContent.ForType(ChocolateType.MildDark), 6);
    }

    [Fact]
    public void Strong_dark_uses_the_curve_at_seventy_percent()
    {
        Assert.Equal(
            MethylxanthineContent.ForCocoaPercent(MethylxanthineContent.StrongDarkAssumedCocoaPercent),
            MethylxanthineContent.ForType(ChocolateType.StrongDark));
    }

    [Theory]
    [InlineData(0, 2.3)]
    [InlineData(20, 2.3)]
    [InlineData(45, 5.6)]
    [InlineData(100, 15.5)]
    [InlineData(10, 2.3)]
    [InlineData(26, 3.092)]
    [InlineData(30, 3.62)]
    [InlineData(40, 4.94)]
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
        var fromPercent = ToxicityCalculator.CalculateFromCocoaPercent(20, 100, 10);
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
