using ChocolateToxicity.Clinical;

namespace ChocolateToxicity.Web.Services;

/// <summary>
/// The patient being worked on: species and weight, entered once and filled into every calculator.
/// </summary>
public sealed class PatientState
{
    public Species Species { get; private set; } = Species.Dog;

    /// <summary>Weight as typed, in kg.</summary>
    public string WeightText { get; private set; } = "";

    public double? WeightKg => Numbers.Parse(WeightText) is > 0 and <= 150 and var kg ? kg : null;

    public event Action? Changed;

    public void SetSpecies(Species species)
    {
        if (species != Species)
        {
            Species = species;
            Changed?.Invoke();
        }
    }

    public void SetWeight(string text)
    {
        if (text != WeightText)
        {
            WeightText = text;
            Changed?.Invoke();
        }
    }

    public void Clear()
    {
        Species = Species.Dog;
        WeightText = "";
        Changed?.Invoke();
    }
}
