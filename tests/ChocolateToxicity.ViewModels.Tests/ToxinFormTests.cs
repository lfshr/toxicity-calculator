using System.Globalization;
using ChocolateToxicity.Core.Toxins;

namespace ChocolateToxicity.ViewModels.Tests;

public class ToxinFormTests
{
    private readonly FakeNavigation _navigation = new();

    public ToxinFormTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-GB");
    }

    [Fact]
    public async Task Home_opens_the_chosen_calculator()
    {
        var home = new HomeViewModel(_navigation);

        await home.ChooseCommand.ExecuteAsync(home.Toxins.Single(t => t.Kind == ToxinKind.Xylitol));

        Assert.Equal(ToxinKind.Xylitol, _navigation.Calculator);
    }

    [Fact]
    public async Task Missing_fields_show_errors_and_no_result()
    {
        var form = new XylitolViewModel(_navigation);

        await form.CalculateCommand.ExecuteAsync(null);

        Assert.Null(_navigation.Shown);
        Assert.NotNull(form.DogWeightError);
        Assert.NotNull(form.ProductEatenError);
        Assert.NotNull(form.XylitolError);
    }

    [Fact]
    public async Task Invalid_weight_alone_blocks_the_result()
    {
        var form = new XylitolViewModel(_navigation)
        {
            DogWeightKg = "0",
            ProductEatenGrams = "10",
            XylitolPer100g = "60",
        };

        await form.CalculateCommand.ExecuteAsync(null);

        Assert.Null(_navigation.Shown);
        Assert.NotNull(form.DogWeightError);
        Assert.Null(form.ProductEatenError);
        Assert.Null(form.XylitolError);
    }

    [Fact]
    public async Task Xylitol_from_label()
    {
        var form = new XylitolViewModel(_navigation)
        {
            DogWeightKg = "20",
            ProductEatenGrams = "10",
            XylitolPer100g = "60",
        };

        await form.CalculateCommand.ExecuteAsync(null);

        var shown = Assert.IsType<ResultViewModel>(_navigation.Shown);
        Assert.Equal("300 mg/kg", shown.Dose);
        Assert.Equal("Hypoglycaemia risk", shown.BandTitle);
        Assert.Equal("10 g x 60 g per 100 g = 6000 mg xylitol, divided by 20 kg.", shown.Working);
    }

    [Fact]
    public async Task Xylitol_from_known_amount()
    {
        var form = new XylitolViewModel(_navigation)
        {
            DogWeightKg = "10",
            UseKnownAmount = true,
            XylitolEatenGrams = "0.5",
        };

        await form.CalculateCommand.ExecuteAsync(null);

        var shown = Assert.IsType<ResultViewModel>(_navigation.Shown);
        Assert.Equal("50 mg/kg", shown.Dose);
        Assert.True(shown.EmesisIndicated);
        Assert.Equal("The dose is at or above the 50 mg/kg threshold for treatment.", shown.VerdictDetail);
    }

    [Fact]
    public async Task Xylitol_label_over_100_is_rejected()
    {
        var form = new XylitolViewModel(_navigation) { DogWeightKg = "10", ProductEatenGrams = "10", XylitolPer100g = "120" };

        await form.CalculateCommand.ExecuteAsync(null);

        Assert.Null(_navigation.Shown);
        Assert.NotNull(form.XylitolError);
    }

    [Fact]
    public async Task Ibuprofen_tablets()
    {
        var form = new PainkillerViewModel(_navigation, Painkiller.Ibuprofen) { DogWeightKg = "8", StrengthMg = "400", Count = "2" };

        await form.CalculateCommand.ExecuteAsync(null);

        var shown = Assert.IsType<ResultViewModel>(_navigation.Shown);
        Assert.Equal("100 mg/kg", shown.Dose);
        Assert.Equal("2 x 400 mg ibuprofen eaten by a 8 kg dog.", shown.Summary);
        Assert.Equal("400 mg x 2 = 800 mg ibuprofen, divided by 8 kg.", shown.Working);
        Assert.Equal(RiskLevel.Moderate, shown.Risk);
    }

    [Fact]
    public void Painkiller_strength_defaults_to_the_usual_tablet()
    {
        Assert.Equal("200", new PainkillerViewModel(_navigation, Painkiller.Ibuprofen).StrengthMg);
        Assert.Equal("500", new PainkillerViewModel(_navigation, Painkiller.Paracetamol).StrengthMg);
        Assert.Equal("Paracetamol", new PainkillerViewModel(_navigation, Painkiller.Paracetamol).Title);
    }

    [Fact]
    public void Paracetamol_clear_restores_the_default_strength()
    {
        var form = new PainkillerViewModel(_navigation, Painkiller.Paracetamol) { DogWeightKg = "8", StrengthMg = "1000", Count = "2" };

        form.ClearCommand.Execute(null);

        Assert.Equal("500", form.StrengthMg);
        Assert.Equal("", form.Count);
        Assert.Equal("", form.DogWeightKg);
    }

    [Fact]
    public void Choosing_a_bait_ingredient_fills_its_usual_concentration()
    {
        var form = new RatBaitViewModel(_navigation) { SelectedIngredient = RodenticideOption.For(RodenticideIngredient.Cholecalciferol) };

        Assert.Equal("0.075", form.ConcentrationPercent);
    }

    [Fact]
    public async Task Rat_bait_result_keeps_small_doses_readable()
    {
        var form = new RatBaitViewModel(_navigation)
        {
            DogWeightKg = "10",
            SelectedIngredient = RodenticideOption.For(RodenticideIngredient.Bromadiolone),
            BaitEatenGrams = "25",
        };

        await form.CalculateCommand.ExecuteAsync(null);

        var shown = Assert.IsType<ResultViewModel>(_navigation.Shown);
        Assert.Equal("0.125 mg/kg", shown.Dose);
        Assert.Equal("25 g x 0.005% = 1.25 mg bromadiolone, divided by 10 kg.", shown.Working);
        Assert.Equal("Consider inducing emesis", shown.Verdict);
        Assert.Equal("The dose is at or above the 0.02 mg/kg threshold for treatment.", shown.VerdictDetail);
        Assert.Equal("Decontaminate and monitor", shown.BandTitle);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Warfarin_is_toxic_from_half_a_mg_per_kg_in_dogs_and_cats(bool cat)
    {
        // 10 g of 0.025% bait in a 4 kg animal is 0.625 mg/kg, above one-tenth of the 5 mg/kg LD50 (Moorman 2002).
        var form = new RatBaitViewModel(_navigation)
        {
            PatientIsCat = cat,
            DogWeightKg = "4",
            SelectedIngredient = RodenticideOption.For(RodenticideIngredient.Warfarin),
            BaitEatenGrams = "10",
        };

        await form.CalculateCommand.ExecuteAsync(null);

        var shown = Assert.IsType<ResultViewModel>(_navigation.Shown);
        Assert.Equal("0.625 mg/kg", shown.Dose);
        Assert.Equal("Toxic dose", shown.BandTitle);
    }

    [Fact]
    public async Task Rat_bait_refuses_a_cat_for_an_ingredient_without_cat_thresholds()
    {
        var form = new RatBaitViewModel(_navigation)
        {
            PatientIsCat = true,
            DogWeightKg = "4",
            SelectedIngredient = RodenticideOption.For(RodenticideIngredient.Brodifacoum),
            BaitEatenGrams = "10",
        };

        await form.CalculateCommand.ExecuteAsync(null);

        Assert.Null(_navigation.Shown);
        Assert.Equal("There are no published cat thresholds for brodifacoum.", form.IngredientError);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Chocolate_by_cocoa_percent_says_the_content_is_an_estimate(bool byPercent)
    {
        var form = new CalculatorViewModel(_navigation, new FakeProductSearch())
        {
            DogWeightKg = "10",
            UseCocoaPercent = byPercent,
            CocoaPercent = "70",
            SelectedChocolateType = ChocolateTypeOption.For(Core.ChocolateType.Milk),
            AmountEatenGrams = "50",
        };

        await form.CalculateCommand.ExecuteAsync(null);

        var shown = Assert.IsType<ResultViewModel>(_navigation.Shown);
        Assert.Equal(byPercent, shown.Working.Contains("an estimate from the cocoa %", StringComparison.Ordinal));
    }

    [Fact]
    public void Chocolate_bands_map_to_risk_colours()
    {
        var result = Core.ToxicityCalculator.Calculate(1, 65, 1);

        Assert.Equal(RiskLevel.Critical, new ResultViewModel(result, 1, 65, "test").Risk);
    }
}
