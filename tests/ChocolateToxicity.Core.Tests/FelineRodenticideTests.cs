using ChocolateToxicity.Core.Toxins;

namespace ChocolateToxicity.Core.Tests;

/// <summary>
/// Cat rat bait thresholds: only where Merola 2002 and Moorman 2002 agree on a cat LD50, and then from the lower of the
/// dog and cat LD50s, because the two papers aren't independent evidence that cats are less sensitive.
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
    public void Ingredients_without_agreed_feline_data_cant_be_assessed_for_a_cat(RodenticideIngredient ingredient)
    {
        Assert.False(Rodenticides.HasCatThresholds(ingredient));
        Assert.Throws<ArgumentException>(() => Rodenticides.ProfileForCat(ingredient));
    }

    [Theory]
    [InlineData(RodenticideIngredient.Warfarin)]
    [InlineData(RodenticideIngredient.Diphacinone)]
    public void Cats_use_the_lower_of_the_dog_and_cat_ld50s(RodenticideIngredient ingredient)
    {
        // For both, the dog LD50 is the lower (warfarin 5 vs 5, diphacinone 0.9 vs 15), so cats get the dog bands.
        var cat = Rodenticides.ProfileForCat(ingredient);
        var dog = Rodenticides.ProfileFor(ingredient);

        Assert.Equal(dog.Bands.Select(b => b.FromDose), cat.Bands.Select(b => b.FromDose));
        Assert.Equal(dog.EmesisFromDose, cat.EmesisFromDose, 9);
        Assert.Contains("lower of the dog and cat LD50s", cat.ThresholdsText, StringComparison.Ordinal);
    }

    [Fact]
    public void Diphacinone_in_a_cat_is_toxic_from_the_dog_dose()
    {
        // The review's example: 50 g of 0.005% bait in a 4 kg cat is 0.625 mg/kg. With the cat LD50 alone this was
        // "decontaminate and monitor"; with the lower of the two it is a toxic dose (from 0.09 mg/kg), as in a dog.
        var mg = Rodenticides.ActiveMgEaten(50, 0.005);
        var assessment = Rodenticides.AssessCat(RodenticideIngredient.Diphacinone, mg, 4);

        Assert.Equal(0.625, assessment.DosePerKg, 9);
        Assert.Equal(RiskLevel.High, assessment.Band.Risk);
    }

    [Theory]
    [InlineData(RodenticideIngredient.Warfarin, "2 weeks")]
    [InlineData(RodenticideIngredient.Diphacinone, "4 weeks")]
    [InlineData(RodenticideIngredient.Brodifacoum, "4 weeks")]
    public void Vitamin_k1_duration_depends_on_the_ingredient(RodenticideIngredient ingredient, string duration)
    {
        Assert.Contains($"for {duration}", Rodenticides.ProfileFor(ingredient).Advice, StringComparison.Ordinal);
    }

    [Fact]
    public void Decontamination_cut_off_cites_moorman_for_cats()
    {
        var band = Rodenticides.ProfileForCat(RodenticideIngredient.Warfarin).Bands[0];

        Assert.Equal(0.02, band.FromDose, 9);
        Assert.Contains("Moorman 2002, for dogs and cats", band.Signs, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(RodenticideIngredient.Difenacoum)]
    [InlineData(RodenticideIngredient.UnknownAnticoagulant)]
    public void Unverified_ld50s_are_a_precaution_not_an_expected_coagulopathy(RodenticideIngredient ingredient)
    {
        var profile = Rodenticides.ProfileFor(ingredient);
        var toxic = profile.Bands.Single(b => b.Risk == RiskLevel.High);

        Assert.Equal(0.0075, toxic.FromDose, 9);
        Assert.Equal("Treat as a potent second-generation bait", toxic.Title);
        Assert.DoesNotContain("expected", toxic.Signs.Replace("not an expected", ""), StringComparison.Ordinal);
    }
}
