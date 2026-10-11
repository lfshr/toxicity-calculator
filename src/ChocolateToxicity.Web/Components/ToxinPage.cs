using ChocolateToxicity.Clinical;
using ChocolateToxicity.ViewModels;

namespace ChocolateToxicity.Web.Components;

/// <summary>A toxin form whose dog's weight follows the shared patient.</summary>
public abstract class ToxinPage<TViewModel> : ViewModelPage<TViewModel>
    where TViewModel : ToxinFormViewModel
{
    /// <summary>Why Cat can't be chosen on this page, or null when it can; see <see cref="SpeciesField.CatUnavailable"/>.</summary>
    protected virtual string? CatUnavailable => null;

    /// <summary>True when the form works for a cat: the patient is a cat and Cat is available here.</summary>
    protected bool IsCat => CatUnavailable is null && Patient.Species == Species.Cat;

    protected override void PullWeightFromPatient()
    {
        Vm.PatientIsCat = IsCat;
        if (Patient.WeightText.Length > 0)
        {
            Vm.DogWeightKg = Patient.WeightText;
        }
    }

    /// <summary>Typing a weight in the form updates the shared patient too, so other calculators get it.</summary>
    protected void OnWeightChanged(string value)
    {
        Vm.DogWeightKg = value;
        Patient.SetWeight(value);
    }

    /// <summary>Clears the form but keeps the patient's weight, which is shared with the other calculators.</summary>
    protected void ClearForm()
    {
        Vm.ClearCommand.Execute(null);
        PullWeightFromPatient();
    }
}
