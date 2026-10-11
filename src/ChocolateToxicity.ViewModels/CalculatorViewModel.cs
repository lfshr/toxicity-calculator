using System.Globalization;
using ChocolateToxicity.Core;
using ChocolateToxicity.ProductSearch;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChocolateToxicity.ViewModels;

/// <summary>
/// The calculator form: dog weight, what was eaten and how much.
/// The chocolate is described either by type or by the cocoa percentage on the label, which product search can fill in.
/// </summary>
public partial class CalculatorViewModel(INavigationService navigation, IProductSearch productSearch) : ObservableObject
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

    /// <summary>Says which product filled in the chocolate, and how.</summary>
    [ObservableProperty]
    public partial string? ProductNote { get; set; }

    /// <summary>Pack weight of the chosen product, offered as the amount eaten.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WholePackText))]
    public partial double? WholePackGrams { get; set; }

    public string? WholePackText => WholePackGrams is { } grams
        ? $"Use whole pack ({grams.ToString("0.#", CultureInfo.CurrentCulture)} g)"
        : null;

    /// <summary>True when the patient is a cat, for the summary; the Android app is for dogs only and leaves this false.</summary>
    public bool PatientIsCat { get; set; }

    /// <summary>
    /// Fills in the chocolate from a product found by search: its cocoa percentage when known,
    /// otherwise the type its category implies, otherwise it asks for the type.
    /// </summary>
    public void ApplyProduct(ChocolateProduct product)
    {
        ChocolateError = null;
        WholePackGrams = product.PackWeightGrams;

        if (product.CocoaPercent is { } cocoa)
        {
            UseCocoaPercent = true;
            CocoaPercent = cocoa.ToString("0.#", CultureInfo.CurrentCulture);
            var from = product.CocoaPercentSource == CocoaPercentSource.ProductName ? "the product name" : "the label data";
            ProductNote = $"{product.DisplayName}: {CocoaPercent}% cocoa, taken from {from}. Check it against the wrapper if you have it.";
        }
        else if (product.SuggestedType is { } type)
        {
            UseCocoaPercent = false;
            SelectedChocolateType = ChocolateTypeOption.For(type);
            ProductNote = $"{product.DisplayName}: listed as {SelectedChocolateType.Description}.";
        }
        else
        {
            UseCocoaPercent = false;
            SelectedChocolateType = null;
            ProductNote = product.Caution is { } caution
                ? $"{product.DisplayName}: {caution}"
                : $"{product.DisplayName}: no cocoa percentage on record. Choose the type of chocolate.";
        }
    }

    [RelayCommand]
    private Task SearchProductsAsync() =>
        navigation.ShowProductSearchAsync(new ProductSearchViewModel(productSearch, navigation, ApplyProduct));

    [RelayCommand]
    private void UseWholePack()
    {
        if (WholePackGrams is { } grams)
        {
            AmountEatenGrams = grams.ToString("0.#", CultureInfo.CurrentCulture);
            AmountEatenError = null;
        }
    }

    [RelayCommand]
    private void Clear()
    {
        ProductNote = null;
        WholePackGrams = null;
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
        var weight = NumberInput.Parse(DogWeightKg);
        DogWeightError = NumberInput.DogWeightError(weight);

        var amount = NumberInput.Parse(AmountEatenGrams);
        AmountEatenError = amount switch
        {
            null => "Enter the amount eaten in grams.",
            < 0 => "Amount can't be negative.",
            _ => null,
        };

        double? cocoa = null;
        if (UseCocoaPercent)
        {
            cocoa = NumberInput.Parse(CocoaPercent);
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

        await navigation.ShowResultAsync(new ResultViewModel(result, weight.Value, amount.Value, description, PatientIsCat ? "cat" : "dog"));
    }
}
