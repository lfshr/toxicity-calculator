namespace ChocolateToxicity.Clinical.Tests;

/// <summary>Cases from the maths audit, checked against the sources by an independent script.</summary>
public class MathAuditTests
{
    [Fact]
    public void Lidocaine_is_first_for_dogs_and_amiodarone_the_alternative()
    {
        var names = EmergencyDrugs.For(Species.Dog).Select(d => d.Name).ToList();

        Assert.True(names.IndexOf("Lidocaine") < names.IndexOf("Amiodarone"));
        Assert.Contains("if lidocaine is unavailable", EmergencyDrugs.For(Species.Dog).Single(d => d.Name == "Amiodarone").Use);
    }

    [Fact]
    public void Cats_get_amiodarone_and_no_lidocaine()
    {
        Assert.DoesNotContain(EmergencyDrugs.For(Species.Cat), d => d.Name == "Lidocaine");
        Assert.Contains("preferred in cats", EmergencyDrugs.For(Species.Cat).Single(d => d.Name == "Amiodarone").Use);
    }

    [Fact]
    public void Atropine_is_a_single_dose()
    {
        Assert.Contains("single dose", EmergencyDrugs.Dog.Single(d => d.Name == "Atropine").Use);
    }

    [Fact]
    public void Biphasic_defibrillation_is_2_then_4_joules_per_kg()
    {
        Assert.Equal((20, 40), EmergencyDrugs.ExternalDefibrillation(10, biphasic: true));
    }

    [Theory]
    [InlineData(1.79, null)]
    [InlineData(1.8, 1)]
    [InlineData(5.05, 1)]
    [InlineData(5.1, 2)]
    [InlineData(100, 8)]
    [InlineData(100.01, null)]
    public void Clevor_drops_at_the_table_edges(double kg, int? drops)
    {
        Assert.Equal(drops, Emetics.Ropinirole(kg)?.Drops);
    }

    [Theory]
    [InlineData(Species.Dog, 0.5, 0.064)]
    [InlineData(Species.Dog, 5, 0.295)]
    [InlineData(Species.Dog, 50, 1.371)]
    [InlineData(Species.Cat, 2, 0.159)]
    [InlineData(Species.Cat, 8, 0.400)]
    public void Body_surface_area_matches_the_Merck_tables(Species species, double kg, double squareMetres)
    {
        Assert.Equal(squareMetres, BodySurfaceArea.SquareMetres(species, kg), 3);
    }

    [Theory]
    [InlineData(Species.Cat, 0.5, 47.6)]
    [InlineData(Species.Dog, 80, 3530.9)]
    public void Maintenance_fluids_at_the_weight_extremes(Species species, double kg, double mlPerDay)
    {
        Assert.Equal(mlPerDay, FluidTherapy.MaintenanceMlPerDay(species, kg), 1);
    }

    [Fact]
    public void Fentanyl_cri_for_a_10_kg_dog()
    {
        Assert.Equal(1, ConstantRateInfusion.UndilutedMlPerHour(10, 5, CriDoseUnit.MicrogramsPerKgPerHour, 0.05), 6);
    }

    [Fact]
    public void Packed_red_cells_for_a_20_kg_dog()
    {
        Assert.Equal(334.3, Transfusion.VolumeMl(20, 12, 25, 70, Transfusion.DefaultBloodVolumeMlPerKg(Species.Dog)), 1);
    }
}
