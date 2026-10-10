using ChocolateToxicity.Core.Toxins;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChocolateToxicity.ViewModels;

/// <summary>A form of grape as shown in the picker.</summary>
public sealed record GrapeFormOption(GrapeForm Form, string Name, string Description)
{
    public static IReadOnlyList<GrapeFormOption> All { get; } =
    [
        new(GrapeForm.Grapes, "Grapes", "grapes"),
        new(GrapeForm.Dried, "Raisins, sultanas or currants", "raisins, sultanas or currants"),
    ];

    public static GrapeFormOption For(GrapeForm form) => All.Single(o => o.Form == form);

    public override string ToString() => Name;
}

/// <summary>The grapes and raisins form: what was eaten and how much.</summary>
public partial class GrapesViewModel(INavigationService navigation) : ToxinFormViewModel(navigation)
{
    public IReadOnlyList<GrapeFormOption> Forms => GrapeFormOption.All;

    [ObservableProperty]
    public partial GrapeFormOption? SelectedForm { get; set; }

    [ObservableProperty]
    public partial string AmountEatenGrams { get; set; } = "";

    [ObservableProperty]
    public partial string? FormError { get; set; }

    [ObservableProperty]
    public partial string? AmountEatenError { get; set; }

    protected override ResultViewModel? BuildResult(double? dogWeightKg)
    {
        FormError = SelectedForm is null ? "Choose grapes or dried fruit." : null;
        (var amount, AmountEatenError) = ParseAmount(AmountEatenGrams, "Enter the amount eaten in grams.");
        if (dogWeightKg is not { } weight || SelectedForm is null || amount is null)
        {
            return null;
        }

        var assessment = GrapesAndRaisins.Assess(SelectedForm.Form, amount.Value, weight);
        return new ResultViewModel(
            assessment,
            $"{Format(amount.Value)} g of {SelectedForm.Description} eaten by a {Format(weight)} kg dog.",
            $"{Format(amount.Value)} g divided by {Format(weight)} kg.");
    }

    protected override void ClearFields()
    {
        SelectedForm = null;
        AmountEatenGrams = "";
        FormError = null;
        AmountEatenError = null;
    }
}
