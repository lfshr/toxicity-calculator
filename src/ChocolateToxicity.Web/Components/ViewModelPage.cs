using System.ComponentModel;
using ChocolateToxicity.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ChocolateToxicity.Web.Components;

/// <summary>
/// A page backed by one of the shared view models. Re-renders when the view model changes (for example when a
/// command sets an error message), and fills the dog's weight in from the shared patient when the form's is blank.
/// </summary>
public abstract class ViewModelPage<TViewModel> : ComponentBase, IDisposable
    where TViewModel : INotifyPropertyChanged
{
    [Inject]
    protected PatientState Patient { get; set; } = default!;

    protected abstract TViewModel Vm { get; }

    protected override void OnInitialized()
    {
        Vm.PropertyChanged += OnViewModelChanged;
        Patient.Changed += OnPatientChanged;
        PullWeightFromPatient();
    }

    /// <summary>Copies the patient's weight into the form when the form has none, or follows the shared patient when it changes.</summary>
    protected virtual void PullWeightFromPatient()
    {
    }

    private void OnPatientChanged()
    {
        PullWeightFromPatient();
        StateHasChanged();
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e) => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        Vm.PropertyChanged -= OnViewModelChanged;
        Patient.Changed -= OnPatientChanged;
    }
}
