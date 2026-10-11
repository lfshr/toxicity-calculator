using ChocolateToxicity.Core.Toxins;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChocolateToxicity.ViewModels;

/// <summary>The ibuprofen or paracetamol form: the strength of each tablet and how many were eaten.</summary>
public partial class PainkillerViewModel : ToxinFormViewModel
{
    public PainkillerViewModel(INavigationService navigation, Painkiller drug)
        : base(navigation)
    {
        Drug = drug;
        DrugName = Painkillers.For(drug).Name;
        DefaultStrength = drug == Painkiller.Ibuprofen ? "200" : "500";
        StrengthMg = DefaultStrength;
    }

    public Painkiller Drug { get; }

    /// <summary>Lower-case drug name, such as "ibuprofen".</summary>
    public string DrugName { get; }

    public string Title => DrugTitle(DrugName);

    public string Intro => $"Estimate the {DrugName} dose a dog has eaten, and whether to consider inducing emesis.";

    /// <summary>Usual UK strengths, as a hint under the strength field.</summary>
    public string StrengthHint => Drug == Painkiller.Ibuprofen
        ? "Usually 200 or 400 mg. For a liquid, enter the mg per mL and the mL swallowed."
        : "Usually 500 mg. For a liquid, enter the mg per mL and the mL swallowed.";

    private string DefaultStrength { get; }

    [ObservableProperty]
    public partial string StrengthMg { get; set; }

    [ObservableProperty]
    public partial string Count { get; set; } = "";

    [ObservableProperty]
    public partial string? StrengthError { get; set; }

    [ObservableProperty]
    public partial string? CountError { get; set; }

    protected override ResultViewModel? BuildResult(double? dogWeightKg)
    {
        var strength = NumberInput.Parse(StrengthMg);
        StrengthError = strength switch
        {
            null => "Enter the strength in mg.",
            <= 0 => "Strength must be more than 0 mg.",
            _ => null,
        };
        (var count, CountError) = ParseAmount(Count, "Enter how many were eaten.");
        if (dogWeightKg is not { } weight || StrengthError is not null || count is null)
        {
            return null;
        }

        var totalMg = strength!.Value * count.Value;
        // Cats are far more sensitive to both drugs, so the dog thresholds are replaced rather than applied.
        return new ResultViewModel(
            PatientIsCat ? Painkillers.AssessCat(Drug, totalMg, weight) : Painkillers.Assess(Drug, totalMg, weight),
            $"{Format(count.Value)} x {Format(strength.Value)} mg {DrugName} eaten by a {Format(weight)} kg {PatientNoun}.",
            $"{Format(strength.Value)} mg x {Format(count.Value)} = {FormatPrecise(totalMg)} mg {DrugName}, divided by {Format(weight)} kg.");
    }

    protected override void ClearFields()
    {
        StrengthMg = DefaultStrength;
        Count = "";
        StrengthError = null;
        CountError = null;
    }

    private static string DrugTitle(string name) => char.ToUpperInvariant(name[0]) + name[1..];
}
