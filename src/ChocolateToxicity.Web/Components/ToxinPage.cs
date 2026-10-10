using ChocolateToxicity.ViewModels;

namespace ChocolateToxicity.Web.Components;

/// <summary>A toxin form whose dog's weight follows the patient header.</summary>
public abstract class ToxinPage<TViewModel> : ViewModelPage<TViewModel>
    where TViewModel : ToxinFormViewModel
{
    protected override void PullWeightFromPatient()
    {
        if (Patient.WeightText.Length > 0)
        {
            Vm.DogWeightKg = Patient.WeightText;
        }
    }

    /// <summary>Typing a weight in the form updates the header too, so other calculators get it.</summary>
    protected void OnWeightChanged(string value)
    {
        Vm.DogWeightKg = value;
        Patient.SetWeight(value);
    }

    /// <summary>Clears the form but keeps the patient's weight, which belongs to the header.</summary>
    protected void ClearForm()
    {
        Vm.ClearCommand.Execute(null);
        PullWeightFromPatient();
    }
}
