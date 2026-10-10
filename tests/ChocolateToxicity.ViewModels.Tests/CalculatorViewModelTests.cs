using System.Globalization;
using ChocolateToxicity.Core;

namespace ChocolateToxicity.ViewModels.Tests;

public class CalculatorViewModelTests
{
    private sealed class FakeNavigation : INavigationService
    {
        public ResultViewModel? Shown { get; private set; }

        public Task ShowResultAsync(ResultViewModel result)
        {
            Shown = result;
            return Task.CompletedTask;
        }
    }

    private readonly FakeNavigation _navigation = new();
    private readonly CalculatorViewModel _viewModel;

    public CalculatorViewModelTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-GB");
        _viewModel = new CalculatorViewModel(_navigation);
    }

    [Fact]
    public async Task Calculates_from_chocolate_type_and_shows_result()
    {
        _viewModel.DogWeightKg = "10";
        _viewModel.AmountEatenGrams = "100";
        _viewModel.SelectedChocolateType = ChocolateTypeOption.For(ChocolateType.Milk);

        await _viewModel.CalculateCommand.ExecuteAsync(null);

        var shown = Assert.IsType<ResultViewModel>(_navigation.Shown);
        Assert.Equal(23, shown.Result.DoseMgPerKg, 6);
        Assert.True(shown.EmesisIndicated);
        Assert.Equal("Consider inducing emesis", shown.Verdict);
        Assert.Equal("23 mg/kg", shown.Dose);
        Assert.Equal("100 g of milk chocolate eaten by a 10 kg dog.", shown.Summary);
    }

    [Fact]
    public async Task Calculates_from_cocoa_percent()
    {
        _viewModel.DogWeightKg = "20";
        _viewModel.AmountEatenGrams = "50";
        _viewModel.UseCocoaPercent = true;
        _viewModel.CocoaPercent = "70";

        await _viewModel.CalculateCommand.ExecuteAsync(null);

        var shown = Assert.IsType<ResultViewModel>(_navigation.Shown);
        var expected = ToxicityCalculator.CalculateFromCocoaPercent(70, 50, 20);
        Assert.Equal(expected.DoseMgPerKg, shown.Result.DoseMgPerKg, 6);
        Assert.Contains("70% cocoa chocolate", shown.Summary);
    }

    [Fact]
    public async Task Low_dose_does_not_indicate_emesis()
    {
        _viewModel.DogWeightKg = "30";
        _viewModel.AmountEatenGrams = "20";
        _viewModel.SelectedChocolateType = ChocolateTypeOption.For(ChocolateType.White);

        await _viewModel.CalculateCommand.ExecuteAsync(null);

        var shown = Assert.IsType<ResultViewModel>(_navigation.Shown);
        Assert.False(shown.EmesisIndicated);
        Assert.Equal("Emesis not indicated by dose", shown.Verdict);
        Assert.Equal(SeverityBand.None, shown.Band);
    }

    [Fact]
    public async Task Missing_inputs_show_errors_and_do_not_navigate()
    {
        await _viewModel.CalculateCommand.ExecuteAsync(null);

        Assert.Null(_navigation.Shown);
        Assert.NotNull(_viewModel.DogWeightError);
        Assert.NotNull(_viewModel.AmountEatenError);
        Assert.Equal("Choose the type of chocolate.", _viewModel.ChocolateError);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("abc")]
    [InlineData("500")]
    public async Task Rejects_invalid_weight(string weight)
    {
        _viewModel.DogWeightKg = weight;
        _viewModel.AmountEatenGrams = "10";
        _viewModel.SelectedChocolateType = ChocolateTypeOption.For(ChocolateType.Milk);

        await _viewModel.CalculateCommand.ExecuteAsync(null);

        Assert.Null(_navigation.Shown);
        Assert.NotNull(_viewModel.DogWeightError);
    }

    [Theory]
    [InlineData("101")]
    [InlineData("-1")]
    [InlineData("")]
    public async Task Rejects_invalid_cocoa_percent(string cocoa)
    {
        _viewModel.DogWeightKg = "10";
        _viewModel.AmountEatenGrams = "10";
        _viewModel.UseCocoaPercent = true;
        _viewModel.CocoaPercent = cocoa;

        await _viewModel.CalculateCommand.ExecuteAsync(null);

        Assert.Null(_navigation.Shown);
        Assert.NotNull(_viewModel.ChocolateError);
    }

    [Fact]
    public async Task Fixing_inputs_clears_errors()
    {
        await _viewModel.CalculateCommand.ExecuteAsync(null);

        _viewModel.DogWeightKg = "10";
        _viewModel.AmountEatenGrams = "10";
        _viewModel.SelectedChocolateType = ChocolateTypeOption.For(ChocolateType.Milk);
        await _viewModel.CalculateCommand.ExecuteAsync(null);

        Assert.Null(_viewModel.DogWeightError);
        Assert.Null(_viewModel.AmountEatenError);
        Assert.Null(_viewModel.ChocolateError);
        Assert.NotNull(_navigation.Shown);
    }

    [Fact]
    public async Task Accepts_a_full_stop_in_a_comma_decimal_culture()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
        _viewModel.DogWeightKg = "12.5";
        _viewModel.AmountEatenGrams = "25,5";
        _viewModel.SelectedChocolateType = ChocolateTypeOption.For(ChocolateType.Milk);

        await _viewModel.CalculateCommand.ExecuteAsync(null);

        var shown = Assert.IsType<ResultViewModel>(_navigation.Shown);
        Assert.Equal(2.3 * 25.5 / 12.5, shown.Result.DoseMgPerKg, 6);
    }

    [Fact]
    public void Product_cocoa_percent_switches_to_cocoa_mode()
    {
        _viewModel.UseProductCocoaPercent(85);

        Assert.True(_viewModel.UseCocoaPercent);
        Assert.Equal("85", _viewModel.CocoaPercent);
    }

    [Fact]
    public async Task Clear_resets_the_form()
    {
        _viewModel.DogWeightKg = "10";
        _viewModel.UseCocoaPercent = true;
        _viewModel.CocoaPercent = "70";
        await _viewModel.CalculateCommand.ExecuteAsync(null);

        _viewModel.ClearCommand.Execute(null);

        Assert.Equal("", _viewModel.DogWeightKg);
        Assert.False(_viewModel.UseCocoaPercent);
        Assert.Equal("", _viewModel.CocoaPercent);
        Assert.Null(_viewModel.AmountEatenError);
    }
}
