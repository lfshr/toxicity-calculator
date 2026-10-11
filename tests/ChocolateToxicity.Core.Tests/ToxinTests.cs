using ChocolateToxicity.Core.Toxins;

namespace ChocolateToxicity.Core.Tests;

public class ToxinTests
{
    [Fact]
    public void Xylitol_from_label()
    {
        // 10 g of gum at 60 g xylitol per 100 g is 6 g of xylitol.
        Assert.Equal(6000, Xylitol.MgEaten(10, 60), 6);
    }

    [Theory]
    [InlineData(99, false, RiskLevel.Low)]
    [InlineData(100, true, RiskLevel.High)]
    [InlineData(499, true, RiskLevel.High)]
    [InlineData(500, true, RiskLevel.Critical)]
    public void Xylitol_thresholds(double dose, bool emesis, RiskLevel risk)
    {
        var assessment = Xylitol.Assess(dose, 1);

        Assert.Equal(emesis, assessment.EmesisIndicated);
        Assert.Equal(risk, assessment.Band.Risk);
    }

    [Theory]
    [InlineData(24, false, RiskLevel.Low)]
    [InlineData(25, true, RiskLevel.Moderate)]
    [InlineData(175, true, RiskLevel.High)]
    [InlineData(400, true, RiskLevel.Critical)]
    public void Ibuprofen_thresholds(double dose, bool emesis, RiskLevel risk)
    {
        var assessment = Painkillers.Assess(Painkiller.Ibuprofen, dose, 1);

        Assert.Equal(emesis, assessment.EmesisIndicated);
        Assert.Equal(risk, assessment.Band.Risk);
    }

    [Fact]
    public void Ibuprofen_tablets_for_a_small_dog()
    {
        // Two 200 mg tablets eaten by an 8 kg dog: 50 mg/kg.
        var assessment = Painkillers.Assess(Painkiller.Ibuprofen, 2 * 200, 8);

        Assert.Equal(50, assessment.DosePerKg, 6);
        Assert.Equal("GI signs expected", assessment.Band.Title);
    }

    [Theory]
    [InlineData(99, false, RiskLevel.Low)]
    [InlineData(100, true, RiskLevel.High)]
    [InlineData(200, true, RiskLevel.Critical)]
    public void Paracetamol_thresholds(double dose, bool emesis, RiskLevel risk)
    {
        var assessment = Painkillers.Assess(Painkiller.Paracetamol, dose, 1);

        Assert.Equal(emesis, assessment.EmesisIndicated);
        Assert.Equal(risk, assessment.Band.Risk);
    }

    [Fact]
    public void Bait_active_ingredient_from_concentration()
    {
        // 0.005% is 0.05 mg per gram of bait.
        Assert.Equal(1.25, Rodenticides.ActiveMgEaten(25, 0.005), 9);
    }

    [Fact]
    public void Brodifacoum_bands_use_a_tenth_of_the_lowest_ld50()
    {
        var profile = Rodenticides.ProfileFor(RodenticideIngredient.Brodifacoum);

        // A tenth of 0.2 mg/kg is 0.02 mg/kg, the same as Merola's threshold, so there's no separate decontaminate band.
        Assert.Equal(new[] { 0.02, 0.2 }, profile.Bands.Select(b => b.FromDose).ToArray());
        Assert.Equal(0.02, profile.EmesisFromDose, 9);
    }

    [Fact]
    public void Bromadiolone_decontaminates_from_merolas_threshold()
    {
        var profile = Rodenticides.ProfileFor(RodenticideIngredient.Bromadiolone);

        Assert.Equal(new[] { 0.02, 1.1, 11 }, profile.Bands.Select(b => b.FromDose).ToArray());
        Assert.Equal(0.02, profile.EmesisFromDose, 9);
        Assert.Equal(RiskLevel.Moderate, profile.Assess(0.5).Band.Risk);
        Assert.Equal(RiskLevel.High, profile.Assess(1.1).Band.Risk);
    }

    [Fact]
    public void Flocoumafen_toxic_dose_is_below_merolas_threshold()
    {
        var profile = Rodenticides.ProfileFor(RodenticideIngredient.Flocoumafen);

        Assert.Equal(0.0075, profile.EmesisFromDose, 9);
        Assert.Equal(RiskLevel.High, profile.Assess(0.01).Band.Risk);
        Assert.Equal(RiskLevel.Critical, profile.Assess(0.075).Band.Risk);
    }

    [Fact]
    public void Unknown_anticoagulant_is_assessed_as_the_most_potent()
    {
        var lowest = Rodenticides.All.Where(i => i.LowestDogLd50MgPerKg is not null).Min(i => i.LowestDogLd50MgPerKg);

        Assert.Equal(lowest, Rodenticides.Info(RodenticideIngredient.UnknownAnticoagulant).LowestDogLd50MgPerKg);
    }

    [Fact]
    public void Rat_bait_for_a_terrier()
    {
        // 25 g of 0.005% brodifacoum bait eaten by a 10 kg dog: 1.25 mg, 0.125 mg/kg.
        var mg = Rodenticides.ActiveMgEaten(25, 0.005);
        var assessment = Rodenticides.Assess(RodenticideIngredient.Brodifacoum, mg, 10);

        Assert.Equal(0.125, assessment.DosePerKg, 9);
        Assert.True(assessment.EmesisIndicated);
        Assert.Equal(RiskLevel.High, assessment.Band.Risk);
    }

    [Theory]
    [InlineData(0.09, false, RiskLevel.Low)]
    [InlineData(0.1, true, RiskLevel.High)]
    [InlineData(0.5, true, RiskLevel.Critical)]
    public void Cholecalciferol_thresholds(double dose, bool emesis, RiskLevel risk)
    {
        var assessment = Rodenticides.Assess(RodenticideIngredient.Cholecalciferol, dose, 1);

        Assert.Equal(emesis, assessment.EmesisIndicated);
        Assert.Equal(risk, assessment.Band.Risk);
    }

    [Fact]
    public void Rejects_bad_input()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Dose.PerKg(-1, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => Dose.PerKg(1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Xylitol.MgEaten(10, 101));
        Assert.Throws<ArgumentOutOfRangeException>(() => Rodenticides.ActiveMgEaten(10, 0));
    }

    [Fact]
    public void Every_profile_cites_its_thresholds()
    {
        ToxinProfile[] profiles =
        [
            Xylitol.Profile, Painkillers.Ibuprofen, Painkillers.Paracetamol,
            .. Rodenticides.All.Select(i => Rodenticides.ProfileFor(i.Ingredient)),
        ];

        Assert.All(profiles, p => Assert.Matches("Merck|Merola|Vet Times|ASPCA", p.ThresholdsText));
    }
}
