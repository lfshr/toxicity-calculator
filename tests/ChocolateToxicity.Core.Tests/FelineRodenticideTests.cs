using ChocolateToxicity.Core.Toxins;

namespace ChocolateToxicity.Core.Tests;

/// <summary>
/// Cat rat bait thresholds: one-tenth of the cat LD50 (Merck), only where Merola 2002 and Moorman 2002 agree on it.
/// </summary>
public class FelineRodenticideTests
{
    [Theory]
    [InlineData(RodenticideIngredient.Warfarin, 5)] // Merola 5-30, Moorman 5-50 mg/kg: lowest 5.
    [InlineData(RodenticideIngredient.Diphacinone, 15)] // Merola and Moorman both 15 mg/kg.
    public void Cat_ld50s_where_the_sources_agree(RodenticideIngredient ingredient, double catLd50)
    {
        Assert.True(Rodenticides.HasCatThresholds(ingredient));
        Assert.Equal(catLd50, Rodenticides.Info(ingredient).LowestCatLd50MgPerKg);
    }

    [Theory]
    [InlineData(RodenticideIngredient.Brodifacoum)] // Merola unknown, Moorman 25 mg/kg.
    [InlineData(RodenticideIngredient.Bromadiolone)] // Merola unknown, Moorman 25 mg/kg.
    [InlineData(RodenticideIngredient.Difethialone)] // Merola only, and only a bound (>16 mg/kg).
    [InlineData(RodenticideIngredient.Difenacoum)]
    [InlineData(RodenticideIngredient.Flocoumafen)]
    [InlineData(RodenticideIngredient.UnknownAnticoagulant)]
    [InlineData(RodenticideIngredient.Cholecalciferol)]
    public void Cats_use_the_dog_thresholds_without_agreed_feline_data(RodenticideIngredient ingredient)
    {
        Assert.False(Rodenticides.HasCatThresholds(ingredient));
        Assert.Equal(
            Rodenticides.ProfileFor(ingredient).ThresholdsText,
            Rodenticides.ProfileForCat(ingredient).ThresholdsText);
        Assert.Equal(
            Rodenticides.ProfileFor(ingredient).Bands.Select(b => b.FromDose),
            Rodenticides.ProfileForCat(ingredient).Bands.Select(b => b.FromDose));
    }

    [Fact]
    public void Warfarin_in_cats_is_toxic_from_a_quarter_of_the_dog_dose()
    {
        var cat = Rodenticides.ProfileForCat(RodenticideIngredient.Warfarin);
        var dog = Rodenticides.ProfileFor(RodenticideIngredient.Warfarin);

        Assert.Equal(new[] { 0.02, 0.5, 5 }, cat.Bands.Select(b => b.FromDose).ToArray());
        Assert.Equal(new[] { 0.02, 2, 20 }, dog.Bands.Select(b => b.FromDose).ToArray());
        Assert.Equal(0.02, cat.EmesisFromDose, 9);
        Assert.Contains("lowest cat LD50 5 mg/kg", cat.ThresholdsText, StringComparison.Ordinal);
    }

    [Fact]
    public void Diphacinone_in_cats_is_toxic_from_1_5_mg_per_kg()
    {
        var cat = Rodenticides.ProfileForCat(RodenticideIngredient.Diphacinone);

        Assert.Equal(new[] { 0.02, 1.5, 15 }, cat.Bands.Select(b => b.FromDose).ToArray());
        Assert.Equal(RiskLevel.Moderate, cat.Assess(1).Band.Risk);
        Assert.Equal(RiskLevel.High, cat.Assess(1.5).Band.Risk);
        Assert.Equal(RiskLevel.Critical, cat.Assess(15).Band.Risk);
    }

    [Fact]
    public void Warfarin_bait_in_a_4_kg_cat()
    {
        // 10 g of 0.025% warfarin bait: 2.5 mg, 0.625 mg/kg. Toxic in a cat (from 0.5), below toxic in a dog (from 2).
        var mg = Rodenticides.ActiveMgEaten(10, 0.025);

        Assert.Equal(0.625, Rodenticides.AssessCat(RodenticideIngredient.Warfarin, mg, 4).DosePerKg, 9);
        Assert.Equal(RiskLevel.High, Rodenticides.AssessCat(RodenticideIngredient.Warfarin, mg, 4).Band.Risk);
        Assert.Equal(RiskLevel.Moderate, Rodenticides.Assess(RodenticideIngredient.Warfarin, mg, 4).Band.Risk);
    }
}
