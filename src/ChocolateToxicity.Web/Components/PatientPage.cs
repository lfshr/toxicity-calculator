using ChocolateToxicity.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ChocolateToxicity.Web.Components;

/// <summary>A calculator page that works from the patient in the header and re-renders when it changes.</summary>
public abstract class PatientPage : ComponentBase, IDisposable
{
    [Inject]
    protected PatientState Patient { get; set; } = default!;

    protected double? WeightKg => Patient.WeightKg;

    protected Clinical.Species Species => Patient.Species;

    protected string SpeciesName => Species == Clinical.Species.Cat ? "cat" : "dog";

    protected override void OnInitialized() => Patient.Changed += Refresh;

    private void Refresh() => InvokeAsync(StateHasChanged);

    protected static double? Parse(string? text) => Numbers.Parse(text);

    protected static string F(double value) => Numbers.Format(value);

    public virtual void Dispose() => Patient.Changed -= Refresh;
}
