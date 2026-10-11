using ChocolateToxicity.Core.Toxins;

namespace ChocolateToxicity.Core.Tests;

/// <summary>Cases from the maths audit, checked against the sources by an independent script.</summary>
public class MathAuditTests
{
    [Fact]
    public void Flocoumafen_at_its_toxic_dose()
    {
        var assessment = Rodenticides.Assess(RodenticideIngredient.Flocoumafen, Rodenticides.ActiveMgEaten(1.5, 0.005), 10);

        Assert.Equal(0.0075, assessment.DosePerKg, 9);
        Assert.Equal("Toxic dose", assessment.Band.Title);
    }

    [Fact]
    public void Brodifacoum_at_its_toxic_dose()
    {
        var assessment = Rodenticides.Assess(RodenticideIngredient.Brodifacoum, Rodenticides.ActiveMgEaten(4, 0.005), 10);

        Assert.Equal(0.02, assessment.DosePerKg, 9);
        Assert.Equal("Toxic dose", assessment.Band.Title);
    }

    [Fact]
    public void Cholecalciferol_at_its_signs_threshold()
    {
        var assessment = Rodenticides.Assess(RodenticideIngredient.Cholecalciferol, Rodenticides.ActiveMgEaten(1, 0.075), 7.5);

        Assert.Equal(0.1, assessment.DosePerKg, 9);
        Assert.Equal("Hypercalcaemia risk", assessment.Band.Title);
    }

    [Fact]
    public void Difenacoum_without_a_verified_ld50_is_assessed_as_an_unknown_anticoagulant()
    {
        var info = Rodenticides.Info(RodenticideIngredient.Difenacoum);
        var unknown = Rodenticides.Info(RodenticideIngredient.UnknownAnticoagulant);

        Assert.False(info.Ld50Verified);
        Assert.Equal(unknown.LowestDogLd50MgPerKg, info.LowestDogLd50MgPerKg);
        Assert.Contains("no verified dog LD50 for difenacoum", Rodenticides.ProfileFor(RodenticideIngredient.Difenacoum).ThresholdsText);
    }

    [Fact]
    public void Cats_have_no_safe_paracetamol_dose()
    {
        var assessment = Painkillers.AssessCat(Painkiller.Paracetamol, 125, 4);

        Assert.True(assessment.EmesisIndicated);
        Assert.Equal("Clinical signs reported", assessment.Band.Title);
        Assert.Equal(RiskLevel.Critical, assessment.Band.Risk);
    }
}
