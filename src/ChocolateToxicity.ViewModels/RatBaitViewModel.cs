using System.Globalization;
using ChocolateToxicity.Core.Toxins;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChocolateToxicity.ViewModels;

/// <summary>A bait active ingredient as shown in the picker.</summary>
public sealed record RodenticideOption(RodenticideInfo Info, string Name)
{
    public static IReadOnlyList<RodenticideOption> All { get; } =
        Rodenticides.All.Select(info => new RodenticideOption(info, info.Ingredient switch
        {
            RodenticideIngredient.UnknownAnticoagulant => "Unknown anticoagulant",
            RodenticideIngredient.Cholecalciferol => "Cholecalciferol (vitamin D3)",
            _ => char.ToUpperInvariant(info.Name[0]) + info.Name[1..],
        })).ToList();

    public static RodenticideOption For(RodenticideIngredient ingredient) => All.Single(o => o.Info.Ingredient == ingredient);

    public override string ToString() => Name;
}

/// <summary>The rat and mouse bait form: the active ingredient, its concentration and how much bait was eaten.</summary>
public partial class RatBaitViewModel(INavigationService navigation) : ToxinFormViewModel(navigation)
{
    public IReadOnlyList<RodenticideOption> Ingredients => RodenticideOption.All;

    [ObservableProperty]
    public partial RodenticideOption? SelectedIngredient { get; set; }

    [ObservableProperty]
    public partial string ConcentrationPercent { get; set; } = "";

    [ObservableProperty]
    public partial string BaitEatenGrams { get; set; } = "";

    [ObservableProperty]
    public partial string? IngredientError { get; set; }

    [ObservableProperty]
    public partial string? ConcentrationError { get; set; }

    [ObservableProperty]
    public partial string? BaitEatenError { get; set; }

    // Fill in the usual concentration for the ingredient; the vet can change it to match the pack.
    partial void OnSelectedIngredientChanged(RodenticideOption? value)
    {
        if (value is not null)
        {
            ConcentrationPercent = value.Info.DefaultConcentrationPercent.ToString("0.####", CultureInfo.CurrentCulture);
            ConcentrationError = null;
            IngredientError = null;
        }
    }

    protected override ResultViewModel? BuildResult(double? dogWeightKg)
    {
        IngredientError = SelectedIngredient switch
        {
            null => "Choose the active ingredient, or Unknown anticoagulant.",
            { } option when PatientIsCat && !Rodenticides.HasCatThresholds(option.Info.Ingredient) =>
                $"There are no published cat thresholds for {option.Info.Name}.",
            _ => null,
        };
        var concentration = NumberInput.Parse(ConcentrationPercent);
        ConcentrationError = concentration switch
        {
            null => "Enter the concentration from the pack.",
            <= 0 or > 100 => "Concentration must be above 0% and at most 100%.",
            _ => null,
        };
        (var bait, BaitEatenError) = ParseAmount(BaitEatenGrams, "Enter the amount of bait eaten in grams.");
        if (dogWeightKg is not { } weight || SelectedIngredient is null || IngredientError is not null || ConcentrationError is not null || bait is null)
        {
            return null;
        }

        var name = SelectedIngredient.Info.Name;
        var activeMg = Rodenticides.ActiveMgEaten(bait.Value, concentration!.Value);
        var percent = concentration.Value.ToString("0.####", CultureInfo.CurrentCulture);
        return new ResultViewModel(
            PatientIsCat
                ? Rodenticides.AssessCat(SelectedIngredient.Info.Ingredient, activeMg, weight)
                : Rodenticides.Assess(SelectedIngredient.Info.Ingredient, activeMg, weight),
            $"{Format(bait.Value)} g of bait containing {percent}% {name}, eaten by a {Format(weight)} kg {PatientNoun}.",
            $"{Format(bait.Value)} g x {percent}% = {FormatPrecise(activeMg)} mg {name}, divided by {Format(weight)} kg.");
    }

    protected override void ClearFields()
    {
        SelectedIngredient = null;
        ConcentrationPercent = "";
        BaitEatenGrams = "";
        IngredientError = null;
        ConcentrationError = null;
        BaitEatenError = null;
    }
}
