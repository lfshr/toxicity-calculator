using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChocolateToxicity.ViewModels;

/// <summary>
/// Shared logic for the toxin forms other than chocolate: the dog's weight, Calculate and Clear.
/// Each form adds its own fields and builds the result.
/// </summary>
public abstract partial class ToxinFormViewModel(INavigationService navigation) : ObservableObject
{
    [ObservableProperty]
    public partial string DogWeightKg { get; set; } = "";

    [ObservableProperty]
    public partial string? DogWeightError { get; set; }

    /// <summary>
    /// Validates the form's own fields, setting their error messages, and builds the result.
    /// Returns null when a field is invalid or <paramref name="dogWeightKg"/> is null (the weight was invalid).
    /// </summary>
    protected abstract ResultViewModel? BuildResult(double? dogWeightKg);

    /// <summary>Clears the form's own fields and their errors.</summary>
    protected abstract void ClearFields();

    [RelayCommand]
    private async Task CalculateAsync()
    {
        var weight = NumberInput.Parse(DogWeightKg);
        DogWeightError = NumberInput.DogWeightError(weight);
        var result = BuildResult(DogWeightError is null ? weight : null);
        if (result is not null)
        {
            await navigation.ShowResultAsync(result);
        }
    }

    [RelayCommand]
    private void Clear()
    {
        DogWeightKg = "";
        DogWeightError = null;
        ClearFields();
    }

    protected static string Format(double value) => NumberInput.Format(value);

    protected static string FormatPrecise(double value) => NumberInput.FormatPrecise(value);

    /// <summary>Parses an amount that must be zero or more, returning the value and its error message.</summary>
    protected static (double? Value, string? Error) ParseAmount(string text, string missing)
    {
        var value = NumberInput.Parse(text);
        return value switch
        {
            null => (null, missing),
            < 0 => (null, "Amount can't be negative."),
            _ => (value, null),
        };
    }
}
