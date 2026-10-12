namespace ChocolateToxicity.Clinical.Tests;

/// <summary>Cases from the clinical source review.</summary>
public class SourceReviewTests
{
    [Theory]
    [InlineData(Species.Dog)]
    [InlineData(Species.Cat)]
    public void Cpr_vasopressors_follow_recover_2024(Species species)
    {
        var drugs = EmergencyDrugs.For(species);
        var vasopressin = drugs.Single(d => d.Name == "Vasopressin");
        var adrenaline = drugs.Single(d => d.Name.StartsWith("Adrenaline", StringComparison.Ordinal));

        Assert.Contains("VF or pulseless VT after a failed shock: suggested first-line vasopressor", vasopressin.Use, StringComparison.Ordinal);
        Assert.Contains("Shockable rhythms: after a failed shock, if vasopressin is unavailable", adrenaline.Use, StringComparison.Ordinal);
        Assert.DoesNotContain("refractory", adrenaline.Use, StringComparison.Ordinal);
        Assert.DoesNotContain("may replace or alternate", vasopressin.Use, StringComparison.Ordinal);
    }

    [Fact]
    public void Atropine_notes_both_recover_doses()
    {
        var atropine = EmergencyDrugs.Dog.Single(d => d.Name == "Atropine");

        Assert.Equal(0.05, atropine.DosePerKg);
        Assert.Contains("0.04 mg/kg", atropine.Use, StringComparison.Ordinal);
    }

    [Fact]
    public void Cat_emetic_is_dexmedetomidine_per_merck()
    {
        // 4 kg cat: 28-160 µg IM (0.056-0.32 mL of 0.5 mg/mL), 14 µg IV (0.028 mL).
        Assert.Equal(28, 4 * Emetics.CatDexmedetomidineImMinMicrogramsPerKg, 9);
        Assert.Equal(160, 4 * Emetics.CatDexmedetomidineImMaxMicrogramsPerKg, 9);
        Assert.Equal(0.028, 4 * Emetics.CatDexmedetomidineIvMicrogramsPerKg / Emetics.DexmedetomidineMicrogramsPerMl, 9);
    }

    [Fact]
    public void Dog_amiodarone_warns_against_polysorbate_80()
    {
        var dog = EmergencyDrugs.Dog.Single(d => d.Name == "Amiodarone");
        var cat = EmergencyDrugs.Cat.Single(d => d.Name == "Amiodarone");

        Assert.Contains("Avoid polysorbate-80 formulations in dogs", dog.Use, StringComparison.Ordinal);
        Assert.Contains("check the vial", dog.Use, StringComparison.Ordinal);
        Assert.DoesNotContain("polysorbate", cat.Use, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false, 40, 60)]
    [InlineData(true, 20, 40)]
    public void Defibrillation_energy_stays_within_recover_ranges(bool biphasic, double first, double later)
    {
        Assert.Equal((first, later), EmergencyDrugs.ExternalDefibrillation(10, biphasic));
    }
}
