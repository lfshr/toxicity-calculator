using ChocolateToxicity.Clinical;
using ChocolateToxicity.ViewModels;

namespace ChocolateToxicity.Web.Components;

/// <summary>A toxin form whose dog's weight follows the shared patient.</summary>
public abstract class ToxinPage<TViewModel> : ViewModelPage<TViewModel>
    where TViewModel : ToxinFormViewModel
{
    protected override void PullWeightFromPatient()
    {
        Vm.PatientIsCat = Patient.Species == Species.Cat;
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
