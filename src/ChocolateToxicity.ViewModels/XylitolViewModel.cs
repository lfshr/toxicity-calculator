using ChocolateToxicity.Core.Toxins;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChocolateToxicity.ViewModels;

/// <summary>
/// The xylitol form. The xylitol eaten comes either from the amount of product and the xylitol content on its label,
/// or from a known amount of xylitol.
/// </summary>
public partial class XylitolViewModel(INavigationService navigation) : ToxinFormViewModel(navigation)
{
    /// <summary>True to enter the xylitol eaten directly rather than from the label.</summary>
    [ObservableProperty]
    public partial bool UseKnownAmount { get; set; }

    [ObservableProperty]
    public partial string ProductEatenGrams { get; set; } = "";

    [ObservableProperty]
    public partial string XylitolPer100g { get; set; } = "";

    [ObservableProperty]
    public partial string XylitolEatenGrams { get; set; } = "";

    [ObservableProperty]
    public partial string? ProductEatenError { get; set; }

    [ObservableProperty]
    public partial string? XylitolError { get; set; }

    protected override ResultViewModel? BuildResult(double? dogWeightKg)
    {
        double? xylitolMg = null;
        string summary;
        string working;
        if (UseKnownAmount)
        {
            ProductEatenError = null;
            (var grams, XylitolError) = ParseAmount(XylitolEatenGrams, "Enter the xylitol eaten in grams.");
            xylitolMg = grams * 1000;
            summary = $"{FormatPrecise(grams ?? 0)} g of xylitol";
            working = $"{FormatPrecise(xylitolMg ?? 0)} mg xylitol";
        }
        else
        {
            (var product, ProductEatenError) = ParseAmount(ProductEatenGrams, "Enter the amount of product eaten in grams.");
            var per100 = NumberInput.Parse(XylitolPer100g);
            XylitolError = per100 switch
            {
                null => "Enter the xylitol per 100 g from the label.",
                < 0 or > 100 => "Xylitol must be between 0 and 100 g per 100 g.",
                _ => null,
            };
            if (product is not null && XylitolError is null)
            {
                xylitolMg = Xylitol.MgEaten(product.Value, per100!.Value);
            }

            summary = $"{Format(product ?? 0)} g of a product with {FormatPrecise(per100 ?? 0)} g xylitol per 100 g";
            working = $"{FormatPrecise(product ?? 0)} g x {FormatPrecise(per100 ?? 0)} g per 100 g = {FormatPrecise(xylitolMg ?? 0)} mg xylitol";
        }

        if (dogWeightKg is not { } weight || xylitolMg is null)
        {
            return null;
        }

        return new ResultViewModel(
            Xylitol.Assess(xylitolMg.Value, weight),
            $"{summary}, eaten by a {Format(weight)} kg dog.",
            $"{working}, divided by {Format(weight)} kg.");
    }

    protected override void ClearFields()
    {
        UseKnownAmount = false;
        ProductEatenGrams = "";
        XylitolPer100g = "";
        XylitolEatenGrams = "";
        ProductEatenError = null;
        XylitolError = null;
    }
}
