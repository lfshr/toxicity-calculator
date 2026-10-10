using System.Globalization;
using ChocolateToxicity.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChocolateToxicity.ViewModels;

/// <summary>
/// The calculator form: dog weight, what was eaten and how much.
/// The chocolate is described either by type or by the cocoa percentage on the label.
/// </summary>
public partial class CalculatorViewModel(INavigationService navigation) : ObservableObject
{
    public IReadOnlyList<ChocolateTypeOption> ChocolateTypes => ChocolateTypeOption.All;

    [ObservableProperty]
    public partial string DogWeightKg { get; set; } = "";

    [ObservableProperty]
    public partial string AmountEatenGrams { get; set; } = "";

    /// <summary>True to describe the chocolate by cocoa percentage rather than by type.</summary>
    [ObservableProperty]
    public partial bool UseCocoaPercent { get; set; }

    [ObservableProperty]
    public partial ChocolateTypeOption? SelectedChocolateType { get; set; }

    [ObservableProperty]
    public partial string CocoaPercent { get; set; } = "";

    [ObservableProperty]
    public partial string? DogWeightError { get; set; }

    [ObservableProperty]
    public partial string? AmountEatenError { get; set; }

    [ObservableProperty]
    public partial string? ChocolateError { get; set; }

    /// <summary>
    /// Fills in the chocolate from a known cocoa percentage, for example from a product found by search.
    /// </summary>
    public void UseProductCocoaPercent(double cocoaPercent)
    {
        UseCocoaPercent = true;
        CocoaPercent = cocoaPercent.ToString("0.#", CultureInfo.CurrentCulture);
        ChocolateError = null;
    }

    [RelayCommand]
    private void Clear()
    {
        DogWeightKg = "";
        AmountEatenGrams = "";
        UseCocoaPercent = false;
        SelectedChocolateType = null;
        CocoaPercent = "";
        DogWeightError = null;
        AmountEatenError = null;
        ChocolateError = null;
    }

    [RelayCommand]
    private async Task CalculateAsync()
    {
        var weight = ParseNumber(DogWeightKg);
        DogWeightError = weight switch
        {
            null => "Enter the dog's weight in kg.",
            <= 0 => "Weight must be more than 0 kg.",
            > 150 => "Check the weight: it should be in kg.",
            _ => null,
        };

        var amount = ParseNumber(AmountEatenGrams);
        AmountEatenError = amount switch
        {
            null => "Enter the amount eaten in grams.",
            < 0 => "Amount can't be negative.",
            _ => null,
        };

        double? cocoa = null;
        if (UseCocoaPercent)
        {
            cocoa = ParseNumber(CocoaPercent);
            ChocolateError = cocoa switch
            {
                null => "Enter the cocoa percentage from the label.",
                < 0 or > 100 => "Cocoa percentage must be between 0 and 100.",
                _ => null,
            };
        }
        else
        {
            ChocolateError = SelectedChocolateType is null ? "Choose the type of chocolate." : null;
        }

        if (DogWeightError is not null || AmountEatenError is not null || ChocolateError is not null)
        {
            return;
        }

        ToxicityResult result;
        string description;
        if (UseCocoaPercent)
        {
            result = ToxicityCalculator.CalculateFromCocoaPercent(cocoa!.Value, amount!.Value, weight!.Value);
            description = $"{cocoa.Value.ToString("0.#", CultureInfo.CurrentCulture)}% cocoa chocolate";
        }
        else
        {
            result = ToxicityCalculator.Calculate(SelectedChocolateType!.Type, amount!.Value, weight!.Value);
            description = SelectedChocolateType.Description;
        }

        await navigation.ShowResultAsync(new ResultViewModel(result, weight.Value, amount.Value, description));
    }

    // Accepts the device's decimal separator, and a full stop as well since many keypads only offer that.
    private static double? ParseNumber(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        const NumberStyles style = NumberStyles.Float;
        if (double.TryParse(text, style, CultureInfo.CurrentCulture, out var value) ||
            double.TryParse(text, style, CultureInfo.InvariantCulture, out value))
        {
            return double.IsFinite(value) ? value : null;
        }

        return null;
    }
}
