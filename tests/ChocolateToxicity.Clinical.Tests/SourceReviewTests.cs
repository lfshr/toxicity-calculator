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

        Assert.Contains("Refractory VF or pulseless VT: preferred first-line vasopressor", vasopressin.Use, StringComparison.Ordinal);
        Assert.Contains("Shockable rhythms: only once refractory", adrenaline.Use, StringComparison.Ordinal);
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
}
