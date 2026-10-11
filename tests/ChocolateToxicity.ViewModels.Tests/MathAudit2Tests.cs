using System.Globalization;
using ChocolateToxicity.Core;
using ChocolateToxicity.Core.Toxins;
using ChocolateToxicity.ProductSearch;

namespace ChocolateToxicity.ViewModels.Tests;

/// <summary>Cases from the second maths audit: the working line in a clinical note must show the inputs as typed.</summary>
public class MathAudit2Tests
{
    private readonly FakeNavigation _navigation = new();

    public MathAudit2Tests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-GB");
    }

    [Fact]
    public async Task Painkiller_working_shows_a_quarter_tablet_and_the_exact_weight()
    {
        var form = new PainkillerViewModel(_navigation, Painkiller.Paracetamol)
        {
            PatientIsCat = true,
            DogWeightKg = "2.25",
            StrengthMg = "500",
            Count = "0.25",
        };

        await form.CalculateCommand.ExecuteAsync(null);

        var shown = Assert.IsType<ResultViewModel>(_navigation.Shown);
        Assert.Equal("0.25 x 500 mg paracetamol eaten by a 2.25 kg cat.", shown.Summary);
        Assert.Equal("500 mg x 0.25 = 125 mg paracetamol, divided by 2.25 kg.", shown.Working);
    }

    [Fact]
    public async Task Painkiller_working_keeps_a_tablet_and_a_quarter()
    {
        var form = new PainkillerViewModel(_navigation, Painkiller.Ibuprofen) { DogWeightKg = "10", StrengthMg = "200", Count = "1.25" };

        await form.CalculateCommand.ExecuteAsync(null);

        var shown = Assert.IsType<ResultViewModel>(_navigation.Shown);
        Assert.StartsWith("200 mg x 1.25 = 250 mg", shown.Working);
    }

    [Fact]
    public async Task Rat_bait_summary_keeps_the_grams_typed_and_names_a_cat()
    {
        var form = new RatBaitViewModel(_navigation)
        {
            PatientIsCat = true,
            DogWeightKg = "4",
            SelectedIngredient = RodenticideOption.For(RodenticideIngredient.Brodifacoum),
            ConcentrationPercent = "0.005",
            BaitEatenGrams = "0.45",
        };

        await form.CalculateCommand.ExecuteAsync(null);

        var shown = Assert.IsType<ResultViewModel>(_navigation.Shown);
        Assert.StartsWith("0.45 g of bait", shown.Summary);
        Assert.EndsWith("kg cat.", shown.Summary);
    }

    [Fact]
    public async Task Xylitol_summary_names_a_cat()
    {
        var form = new XylitolViewModel(_navigation) { PatientIsCat = true, DogWeightKg = "4", UseKnownAmount = true, XylitolEatenGrams = "1" };

        await form.CalculateCommand.ExecuteAsync(null);

        var shown = Assert.IsType<ResultViewModel>(_navigation.Shown);
        Assert.EndsWith("4 kg cat.", shown.Summary);
    }

    [Fact]
    public async Task Chocolate_summary_names_a_cat_and_keeps_the_weight_typed()
    {
        var form = new CalculatorViewModel(_navigation, new FakeProductSearch())
        {
            PatientIsCat = true,
            DogWeightKg = "4.25",
            AmountEatenGrams = "12.5",
            SelectedChocolateType = ChocolateTypeOption.For(ChocolateType.Milk),
        };

        await form.CalculateCommand.ExecuteAsync(null);

        var shown = Assert.IsType<ResultViewModel>(_navigation.Shown);
        Assert.Equal("12.5 g of milk chocolate eaten by a 4.25 kg cat.", shown.Summary);
        Assert.EndsWith("divided by 4.25 kg.", shown.Working);
    }

    [Fact]
    public void Chocolate_thresholds_say_when_it_is_potentially_lethal()
    {
        var shown = new ResultViewModel(ToxicityCalculator.Calculate(ChocolateType.Baking, 100, 10), 10, 100, "baking chocolate");

        Assert.Contains("potentially lethal from 100 mg/kg", shown.ThresholdsText);
        Assert.Contains("Potentially lethal from 100 mg/kg", shown.ExpectedSigns);
    }

    [Fact]
    public void A_hazelnut_dark_bar_from_search_goes_by_the_dark_type()
    {
        var form = new CalculatorViewModel(_navigation, new FakeProductSearch());
        var product = OpenFoodFactsProductSearch.ParseProduct(System.Text.Json.JsonDocument.Parse(
            """{ "code": "9", "product_name": "Dark chocolate with 20% hazelnuts", "categories_tags": ["en:dark-chocolates"] }""").RootElement)!;

        form.ApplyProduct(product);

        Assert.False(form.UseCocoaPercent);
        Assert.Equal(ChocolateType.StrongDark, form.SelectedChocolateType?.Type);
    }

    [Fact]
    public void Ambiguous_100_percent_cocoa_from_search_asks_for_the_type()
    {
        var form = new CalculatorViewModel(_navigation, new FakeProductSearch());
        var product = OpenFoodFactsProductSearch.ParseProduct(System.Text.Json.JsonDocument.Parse(
            """{ "code": "9", "product_name": "Pure cacao", "nutriments": { "cocoa_100g": 100 }, "categories_tags": ["en:cocoa-and-its-products"] }""").RootElement)!;

        form.ApplyProduct(product);

        Assert.Null(form.SelectedChocolateType);
        Assert.Contains("Cocoa powder", form.ProductNote);
    }
}
