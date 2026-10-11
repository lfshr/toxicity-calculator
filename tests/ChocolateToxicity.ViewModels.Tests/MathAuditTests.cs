using System.Globalization;
using ChocolateToxicity.Core;
using ChocolateToxicity.Core.Toxins;

namespace ChocolateToxicity.ViewModels.Tests;

/// <summary>Cases from the maths audit: what a clinician reads must agree with itself and with the sources.</summary>
public class MathAuditTests
{
    private readonly FakeNavigation _navigation = new();

    public MathAuditTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-GB");
    }

    [Theory]
    [InlineData(Painkiller.Paracetamol, "500", "0.25", "31.3 mg/kg", "There is no established safe dose of paracetamol in cats.")]
    [InlineData(Painkiller.Ibuprofen, "200", "1", "50 mg/kg", "There is no established safe dose of ibuprofen in cats.")]
    public async Task Painkillers_in_cats_never_say_emesis_is_not_indicated(
        Painkiller drug, string strength, string count, string dose, string detail)
    {
        var form = new PainkillerViewModel(_navigation, drug)
        {
            PatientIsCat = true,
            DogWeightKg = "4",
            StrengthMg = strength,
            Count = count,
        };

        await form.CalculateCommand.ExecuteAsync(null);

        var shown = Assert.IsType<ResultViewModel>(_navigation.Shown);
        Assert.Equal(dose, shown.Dose);
        Assert.True(shown.EmesisIndicated);
        Assert.Equal("Consider inducing emesis", shown.Verdict);
        Assert.Equal(detail, shown.VerdictDetail);
        Assert.Contains("kg cat", shown.Summary);
        Assert.DoesNotContain("25 mg/kg threshold", shown.VerdictDetail);
    }

    [Fact]
    public async Task Paracetamol_in_a_dog_still_uses_the_dog_thresholds()
    {
        var form = new PainkillerViewModel(_navigation, Painkiller.Paracetamol) { DogWeightKg = "4", StrengthMg = "500", Count = "0.25" };

        await form.CalculateCommand.ExecuteAsync(null);

        var shown = Assert.IsType<ResultViewModel>(_navigation.Shown);
        Assert.False(shown.EmesisIndicated);
        Assert.Contains("kg dog", shown.Summary);
    }

    [Theory]
    [InlineData(ChocolateType.Milk, 86.9, 10, "20 mg/kg")]
    [InlineData(ChocolateType.Milk, 100, 11.5, "20 mg/kg")]
    public void Chocolate_dose_and_verdict_agree_at_the_threshold(ChocolateType type, double grams, double kg, string dose)
    {
        var shown = new ResultViewModel(ToxicityCalculator.Calculate(type, grams, kg), kg, grams, "milk chocolate");

        Assert.Equal(dose, shown.Dose);
        Assert.True(shown.EmesisIndicated);
        Assert.Equal(SeverityBand.Mild, shown.Band);
    }

    [Fact]
    public async Task Xylitol_dose_and_verdict_agree_at_the_threshold()
    {
        var form = new XylitolViewModel(_navigation) { DogWeightKg = "10", UseKnownAmount = true, XylitolEatenGrams = "0.9996" };

        await form.CalculateCommand.ExecuteAsync(null);

        var shown = Assert.IsType<ResultViewModel>(_navigation.Shown);
        Assert.Equal("100 mg/kg", shown.Dose);
        Assert.True(shown.EmesisIndicated);
    }

    [Fact]
    public async Task Ibuprofen_dose_and_verdict_agree_at_the_threshold()
    {
        var form = new PainkillerViewModel(_navigation, Painkiller.Ibuprofen) { DogWeightKg = "10", StrengthMg = "200", Count = "1.249" };

        await form.CalculateCommand.ExecuteAsync(null);

        var shown = Assert.IsType<ResultViewModel>(_navigation.Shown);
        Assert.Equal("25 mg/kg", shown.Dose);
        Assert.True(shown.EmesisIndicated);
    }

    [Theory]
    [InlineData(RodenticideIngredient.Brodifacoum, "0.005", "3.999", "0.02 mg/kg")]
    [InlineData(RodenticideIngredient.Cholecalciferol, "0.075", "1.33", "0.1 mg/kg")]
    public async Task Rat_bait_dose_and_verdict_agree_at_the_threshold(RodenticideIngredient ingredient, string percent, string grams, string dose)
    {
        var form = new RatBaitViewModel(_navigation)
        {
            DogWeightKg = "10",
            SelectedIngredient = RodenticideOption.All.Single(o => o.Info.Ingredient == ingredient),
            ConcentrationPercent = percent,
            BaitEatenGrams = grams,
        };

        await form.CalculateCommand.ExecuteAsync(null);

        var shown = Assert.IsType<ResultViewModel>(_navigation.Shown);
        Assert.Equal(dose, shown.Dose);
        Assert.True(shown.EmesisIndicated);
    }

    [Fact]
    public void White_chocolate_working_shows_its_content()
    {
        var shown = new ResultViewModel(ToxicityCalculator.Calculate(ChocolateType.White, 100, 10), 10, 100, "white chocolate");

        Assert.StartsWith("0.04 mg/g x 100 g = 4 mg", shown.Working);
    }

    [Fact]
    public async Task Xylitol_working_shows_a_small_label_value()
    {
        var form = new XylitolViewModel(_navigation) { DogWeightKg = "10", ProductEatenGrams = "3", XylitolPer100g = "0.25" };

        await form.CalculateCommand.ExecuteAsync(null);

        var shown = Assert.IsType<ResultViewModel>(_navigation.Shown);
        Assert.StartsWith("3 g x 0.25 g per 100 g = 7.5 mg xylitol", shown.Working);
    }

    [Fact]
    public void Dark_chocolate_of_unknown_strength_says_what_it_assumes()
    {
        var option = ChocolateTypeOption.For(ChocolateType.StrongDark);

        Assert.Contains("assumes 70%", option.Name);
        Assert.Equal(MethylxanthineContent.ForCocoaPercent(70), MethylxanthineContent.ForType(ChocolateType.StrongDark));
    }
}
